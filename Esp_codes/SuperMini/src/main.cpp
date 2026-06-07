#include <Arduino.h>
#include <WiFi.h>
#include <PubSubClient.h>
#include <ArduinoJson.h>
#include <Preferences.h>
#include "time.h"
#include <HX711.h>

// --- Global Configurations ---
Preferences preferences;
String ssid = "";
String password = "";
String mqtt_server = ""; // Loaded dynamically from NVS

const char* publish_topic = "esp32/scale/telemetry";
const char* ntpServer = "pool.ntp.org";
const long  gmtOffset_sec = 0;
const int   daylightOffset_sec = 0;

WiFiClient espClient;
PubSubClient client(espClient);
unsigned long lastMsg = 0;
unsigned long lastReconnectAttempt = 0;

// --- HX711 Configuration ---
const int HX711_DOUT_LEFT = 3; 
const int HX711_SCK_LEFT  = 4; 
const int HX711_DOUT_RIGHT = 5;
const int HX711_SCK_RIGHT  = 6;

HX711 scaleLeft;
HX711 scaleRight;

// --- Function Prototypes ---
void sync_time();
void reconnect();
void serial_config_task(void *pvParameters);
bool testWifiConnection(String testSsid, String testPass);

void setup() {
  Serial.begin(115200);
  
  scaleLeft.begin(HX711_DOUT_LEFT, HX711_SCK_LEFT);
  scaleRight.begin(HX711_DOUT_RIGHT, HX711_SCK_RIGHT);
  
  Serial.println("[ESP] Scales initialized. Taring... do not apply weight.");
  scaleLeft.set_scale(16000.f);
  scaleRight.set_scale(23800.f);
  scaleLeft.tare();
  scaleRight.tare();
  Serial.println("[ESP] Taring complete.");

  xTaskCreate(serial_config_task, "serial_config_task", 4096, NULL, 5, NULL);

  preferences.begin("wifi_creds", true);
  ssid = preferences.getString("ssid", "");
  password = preferences.getString("password", "");
  mqtt_server = preferences.getString("mqtt_server", "");
  preferences.end();

  if (ssid.length() > 0) {
    Serial.printf("\n[ESP] Connecting to SSID: %s\n", ssid.c_str());
    WiFi.begin(ssid.c_str(), password.c_str());
    
    int attempts = 0;
    while (WiFi.status() != WL_CONNECTED && attempts < 20) {
      delay(500);
      Serial.print(".");
      attempts++;
    }

    if (WiFi.status() == WL_CONNECTED) {
      Serial.printf("\n[ESP] WiFi Connected!\n");
      Serial.printf("[ESP] IP Address: ");
      Serial.println(WiFi.localIP());
      
      sync_time();
      
      if (mqtt_server.length() > 0) {
        Serial.printf("[ESP] Setting MQTT Broker to: %s\n", mqtt_server.c_str());
        client.setServer(mqtt_server.c_str(), 1883);
      } else {
        Serial.printf("[ESP] No MQTT Broker IP found.\n");
      }
    } else {
      Serial.printf("\n[ESP] WiFi Connection Failed.\n");
    }
  } else {
    Serial.printf("\n[ESP] No WiFi credentials found.\n");
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
          else if (inputBuffer.startsWith("TARE")) {
            Serial.println("[ESP] Taring scales...");
            scaleLeft.tare();
            scaleRight.tare();
            Serial.println("[ESP] Taring complete.");
          }
          inputBuffer = "";
        }
      } else {
        inputBuffer += c; 
      }
    }
    vTaskDelay(10 / portTICK_PERIOD_MS); 
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
  while (time(nullptr) < 1000) {
    delay(500);
    Serial.print(".");
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
  if (WiFi.status() == WL_CONNECTED && mqtt_server.length() > 0) {
    if (!client.connected()) {
      unsigned long now = millis();
      if (now - lastReconnectAttempt > 5000) {
        lastReconnectAttempt = now;
        reconnect();
      }
    } else {
      client.loop();
      unsigned long now = millis();
      if (now - lastMsg >= 300) {
        lastMsg = now;
        
        float weightLeft = 0.0;
        if (scaleLeft.is_ready()) {
          weightLeft = scaleLeft.get_units(1) * -1; // Invert signal
        }
        
        float weightRight = 0.0;
        if (scaleRight.is_ready()) {
          weightRight = scaleRight.get_units(1)*1;
        }
        
        struct timeval tv;
        gettimeofday(&tv, NULL);
        
        JsonDocument docLeft;
        docLeft["deviceId"] = "Left";
        docLeft["weight"] = weightLeft;
        docLeft["timestamp_s"] = (uint64_t)tv.tv_sec;
        docLeft["timestamp_ms"] = (uint16_t)(tv.tv_usec / 1000);
        char jsonBufferLeft[256];
        serializeJson(docLeft, jsonBufferLeft);
        client.publish(publish_topic, jsonBufferLeft);

        JsonDocument docRight;
        docRight["deviceId"] = "Right";
        docRight["weight"] = weightRight;
        docRight["timestamp_s"] = (uint64_t)tv.tv_sec;
        docRight["timestamp_ms"] = (uint16_t)(tv.tv_usec / 1000);
        char jsonBufferRight[256];
        serializeJson(docRight, jsonBufferRight);
        client.publish(publish_topic, jsonBufferRight);
      }
    }
  }
}