#include <Arduino.h>
#include <WiFi.h>
#include <PubSubClient.h>
#include <ArduinoJson.h>
#include <Preferences.h>
#include "time.h"
#include <HX711.h>
#include <math.h>

// ==========================================
// CONFIGURATION: Set to true for real sensors, false for fake sine wave
// ==========================================
const bool USE_HARDWARE_SCALES = false; 

// --- Global Configurations ---
Preferences preferences;
String ssid = "";
String password = "";
String mqtt_server = ""; 

const char* publish_topic = "esp32/scale/telemetry";
const char* ntpServer = "pool.ntp.org";
const long  gmtOffset_sec = 0;
const int   daylightOffset_sec = 0;

WiFiClient espClient;
PubSubClient client(espClient);
unsigned long lastReconnectAttempt = 0;

// --- Batching Configuration ---
const int BATCH_SIZE = 100; // Keeping your change to 100
float batchLeft[BATCH_SIZE];
float batchRight[BATCH_SIZE];
int batchIndex = 0;

// --- HX711 Configuration ---
const int HX711_DOUT_LEFT = 3; 
const int HX711_SCK_LEFT  = 4; 
const int HX711_DOUT_RIGHT = 5;
const int HX711_SCK_RIGHT  = 6;

HX711 scaleLeft;
HX711 scaleRight;

// --- Fake Data Variables ---
unsigned long lastSimulatedRead = 0;

// --- Function Prototypes ---
void sync_time();
void reconnect();
void serial_config_task(void *pvParameters);
bool testWifiConnection(String testSsid, String testPass);

void setup() {
  Serial.begin(115200);
  delay(1000); 

  // *** THE FIX IS HERE ***
  // Increase buffer size to handle large JSON payloads from BATCH_SIZE = 100
  client.setBufferSize(2048); 

  xTaskCreatePinnedToCore(
      serial_config_task, 
      "serial_config_task", 
      4096, 
      NULL, 
      1,
      NULL,
      0
  );

  if (USE_HARDWARE_SCALES) {
    Serial.println("[ESP] Hardware Mode: Initializing HX711...");
    scaleLeft.begin(HX711_DOUT_LEFT, HX711_SCK_LEFT);
    scaleRight.begin(HX711_DOUT_RIGHT, HX711_SCK_RIGHT);
    
    Serial.println("[ESP] Scales initialized. Taring... do not apply weight.");
    scaleLeft.set_scale(16000.f);
    scaleRight.set_scale(23800.f);
    
    scaleLeft.tare();
    scaleRight.tare();
    Serial.println("[ESP] Taring complete.");
  } else {
    Serial.println("[ESP] Simulation Mode: HX711 bypassed. Generating Sine Waves.");
  }

  preferences.begin("wifi_creds", true);
  ssid = preferences.getString("ssid", "");
  password = preferences.getString("password", "");
  mqtt_server = preferences.getString("mqtt_server", "");
  preferences.end();

  if (ssid.length() > 0) {
    Serial.printf("\n[ESP] Connecting to SSID: %s\n", ssid.c_str());
    WiFi.begin(ssid.c_str(), password.c_str());
  } else {
    Serial.printf("\n[ESP] No WiFi credentials found. Waiting for configuration via Serial.\n");
  }
}

void serial_config_task(void *pvParameters) {
  String inputBuffer = "";
  while (1) {
    while (Serial.available() > 0) {
      char c = Serial.read();
      if (c == '\n' || c == '\r') {
        if (inputBuffer.length() > 0) {
          if (inputBuffer.startsWith("PING")) {
            Serial.println("START_APLIKACJA");
          } 
          else if (inputBuffer.startsWith("WIFI_CONFIG:")) {
            int firstColon = inputBuffer.indexOf(':');
            int secondColon = inputBuffer.indexOf(':', firstColon + 1);
            if (firstColon > 0 && secondColon > firstColon) {
              String newSsid = inputBuffer.substring(firstColon + 1, secondColon);
              String newPass = inputBuffer.substring(secondColon + 1);
              if (testWifiConnection(newSsid, newPass)) {
                preferences.begin("wifi_creds", false);
                preferences.putString("ssid", newSsid);
                preferences.putString("password", newPass);
                preferences.end();
                Serial.println("[ESP] WIFI_CONFIRMED");
              } else {
                Serial.println("[ESP] WIFI_FAILED");
              }
            }
          }
          else if (inputBuffer.startsWith("MQTT_CONFIG:")) {
            String newMqtt = inputBuffer.substring(12); 
            newMqtt.trim();
            if (newMqtt.length() > 0) {
              preferences.begin("wifi_creds", false);
              preferences.putString("mqtt_server", newMqtt);
              preferences.end();
              mqtt_server = newMqtt;
              client.setServer(mqtt_server.c_str(), 1883);
              if (client.connected()) client.disconnect();
              Serial.println("[ESP] MQTT_CONFIRMED");
            }
          }
          else if (inputBuffer.startsWith("DISCONNECT_CMD")) {
               preferences.begin("wifi_creds", false);
               preferences.clear();
               preferences.end();
               Serial.println("DISCONNECTED_OK");
               delay(500);
               ESP.restart();
          }
          else if (inputBuffer.startsWith("TARE")) {
            if (USE_HARDWARE_SCALES) {
                Serial.println("[ESP] Taring scales...");
                scaleLeft.tare();
                scaleRight.tare();
                Serial.println("[ESP] Taring complete.");
            } else {
                Serial.println("[ESP] Simulation Mode: Tare ignored.");
            }
          }
          inputBuffer = "";
        }
      } else {
        inputBuffer += c; 
        if (inputBuffer.length() > 256) {
          inputBuffer = ""; 
        }
      }
    }
    vTaskDelay(50 / portTICK_PERIOD_MS);
  }
}

bool testWifiConnection(String testSsid, String testPass) {
  WiFi.disconnect();
  delay(100);
  WiFi.begin(testSsid.c_str(), testPass.c_str());
  int attempts = 0;
  while (WiFi.status() != WL_CONNECTED && attempts < 20) {
    delay(500); 
    Serial.print(".");
    attempts++;
  }
  Serial.println();
  return WiFi.status() == WL_CONNECTED;
}

void sync_time() {
  configTime(gmtOffset_sec, daylightOffset_sec, ntpServer);
  int attempts = 0;
  while (time(nullptr) < 1000 && attempts < 10) {
    delay(500);
    Serial.print(".");
    attempts++;
  }
  Serial.println();
}

void reconnect() {
  if (mqtt_server.length() == 0) return;
  String clientId = "ESP32_Scales-" + String(random(0xffff), HEX);
  if (client.connect(clientId.c_str())) {
    Serial.println("[ESP] Connected to .NET Broker!");
  } else {
    Serial.printf("[ESP] Failed, rc=%d\n", client.state());
  }
}

void loop() {
  if (WiFi.status() == WL_CONNECTED) {
      if (mqtt_server.length() > 0) {
        if (!client.connected()) {
          unsigned long now = millis();
          if (now - lastReconnectAttempt > 5000) {
            lastReconnectAttempt = now;
            reconnect();
          }
        } else {
          client.loop();

          bool gotNewData = false;

          if (USE_HARDWARE_SCALES) {
              if (scaleLeft.is_ready() && scaleRight.is_ready()) {
                batchLeft[batchIndex] = scaleLeft.get_units(1) * -1.0f;
                batchRight[batchIndex] = scaleRight.get_units(1) * 1.0f;
                gotNewData = true;
              }
          } else {
              if (millis() - lastSimulatedRead >= 12) {
                  lastSimulatedRead = millis();
                  float timeSec = millis() / 1000.0;
                  batchLeft[batchIndex] = 100.0 + 100.0 * sin(2.0 * PI * 0.5 * timeSec);
                  batchRight[batchIndex] = 100.0 + 100.0 * cos(2.0 * PI * 0.5 * timeSec);
                  gotNewData = true;
              }
          }

          if (gotNewData) {
            batchIndex++;

            if (batchIndex >= BATCH_SIZE) {
              struct timeval tv;
              gettimeofday(&tv, NULL);

              JsonDocument doc;
              doc["timestamp_s"] = (uint64_t)tv.tv_sec;
              doc["timestamp_ms"] = (uint16_t)(tv.tv_usec / 1000);
              
              JsonArray leftArray = doc["left"].to<JsonArray>();
              JsonArray rightArray = doc["right"].to<JsonArray>();
              
              for (int i = 0; i < BATCH_SIZE; i++) {
                leftArray.add(batchLeft[i]);
                rightArray.add(batchRight[i]);
              }

              char jsonBuffer[2048]; // Match the buffer size
              serializeJson(doc, jsonBuffer);
              client.publish(publish_topic, jsonBuffer);
              
              batchIndex = 0; 
            }
          }
        }
      }
  } else {
      delay(100); 
  }
}