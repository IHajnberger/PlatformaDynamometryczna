#include <Arduino.h>
#include <WiFi.h>
#include <PubSubClient.h>
#include <ArduinoJson.h>
#include <Preferences.h>
#include "time.h"

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

// UNIQUE IDENTIFIER FOR THIS SPECIFIC ESP32
// Change this to "Right" before flashing your second board!
const char* deviceId = "Left";

// --- Function Prototypes ---
void sync_time();
unsigned long get_epoch_time();
void reconnect();
void serial_config_task(void *pvParameters);
bool testWifiConnection(String testSsid, String testPass);

void setup() {
  Serial.begin(115200);
  
  // 1. Start the Serial Listener Task immediately
  xTaskCreate(serial_config_task, "serial_config_task", 4096, NULL, 5, NULL);

  // 2. Load Credentials from NVS
  preferences.begin("wifi_creds", true); // true = read-only mode
  ssid = preferences.getString("ssid", "");
  password = preferences.getString("password", "");
  mqtt_server = preferences.getString("mqtt_server", ""); // Load broker IP
  preferences.end();

  // 3. Attempt WiFi Connection
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
      Serial.println("\n[ESP] WiFi Connected!");
      Serial.print("[ESP] IP Address: ");
      Serial.println(WiFi.localIP());
      
      sync_time();
      
      // Only set up MQTT if we actually have an IP saved
      if (mqtt_server.length() > 0) {
        Serial.printf("[ESP] Setting MQTT Broker to: %s\n", mqtt_server.c_str());
        client.setServer(mqtt_server.c_str(), 1883);
      } else {
        Serial.println("[ESP] No MQTT Broker IP found. Waiting for configuration.");
      }
    } else {
      Serial.println("\n[ESP] WiFi Connection Failed. Waiting for new configuration.");
    }
  } else {
    Serial.println("\n[ESP] No WiFi credentials found. Waiting for configuration.");
  }
}

// --- Background Task: Serial Command Listener ---
void serial_config_task(void *pvParameters) {
  String inputBuffer = "";
  
  while (1) {
    while (Serial.available() > 0) {
      char c = Serial.read();
      
      if (c == '\n' || c == '\r') {
        if (inputBuffer.length() > 0) {
          
          // 1. Handshake
          if (inputBuffer.startsWith("PING")) {
            Serial.println("START_APLIKACJA");
          } 
          
          // 2. WiFi Configuration Command
          else if (inputBuffer.startsWith("WIFI_CONFIG:")) {
            int firstColon = inputBuffer.indexOf(':');
            int secondColon = inputBuffer.indexOf(':', firstColon + 1);

            if (firstColon > 0 && secondColon > firstColon) {
              String newSsid = inputBuffer.substring(firstColon + 1, secondColon);
              String newPass = inputBuffer.substring(secondColon + 1);

              Serial.printf("[ESP] Testing new WiFi Config... SSID: %s\n", newSsid.c_str());

              if (testWifiConnection(newSsid, newPass)) {
                // Connection successful! Save credentials
                preferences.begin("wifi_creds", false);
                preferences.putString("ssid", newSsid);
                preferences.putString("password", newPass);
                preferences.end();

                Serial.println("[ESP] WIFI_CONFIRMED");
                // DO NOT RESTART HERE! Wait for MQTT config.
              } else {
                // Connection failed
                Serial.println("[ESP] WIFI_FAILED");
              }
            }
          }
          
          // 3. MQTT Configuration Command
          else if (inputBuffer.startsWith("MQTT_CONFIG:")) {
            // Expected format: MQTT_CONFIG:192.168.1.50
            String newMqtt = inputBuffer.substring(12); 
            newMqtt.trim(); // Clean up hidden return characters

            if (newMqtt.length() > 0) {
              Serial.printf("[ESP] Saving and applying new MQTT Broker... IP: %s\n", newMqtt.c_str());

              // Save to permanent memory
              preferences.begin("wifi_creds", false);
              preferences.putString("mqtt_server", newMqtt);
              preferences.end();

              // Apply the new settings immediately in memory
              mqtt_server = newMqtt;
              client.setServer(mqtt_server.c_str(), 1883);
              
              // Force disconnect if it was connected to an old broker
              if (client.connected()) {
                client.disconnect();
              }

              Serial.println("[ESP] MQTT_CONFIRMED");
              // No restart needed. The main loop will now attempt to connect.
            }
          }
          
          inputBuffer = ""; // Reset buffer
        }
      } else {
        inputBuffer += c; 
      }
    }
    vTaskDelay(10 / portTICK_PERIOD_MS); 
  }
}

// --- Helper Functions ---
bool testWifiConnection(String testSsid, String testPass) {
  // Disconnect from current WiFi if connected
  WiFi.disconnect();
  delay(100);

  Serial.println("[ESP] Attempting to connect to new WiFi...");
  WiFi.begin(testSsid.c_str(), testPass.c_str());

  int attempts = 0;
  // Wait up to 10 seconds for connection (20 * 500ms)
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
  Serial.print("[ESP] Waiting for NTP time sync...");
  struct tm timeinfo;
  while (!getLocalTime(&timeinfo)) {
    delay(500);
    Serial.print(".");
  }
  Serial.println("\n[ESP] Time Synced Successfully!");
}

unsigned long get_epoch_time() {
  time_t now;
  struct tm timeinfo;
  if (!getLocalTime(&timeinfo)) {
    return 0;
  }
  time(&now);
  return now;
}

void reconnect() {
  // Safety guard: Do not attempt connection without a broker IP
  if (mqtt_server.length() == 0) return;

  Serial.print("[ESP] Attempting MQTT connection...");
  String clientId = String(deviceId) + "-" + String(random(0xffff), HEX);
  
  if (client.connect(clientId.c_str())) {
    Serial.println("[ESP] Connected to .NET Broker!");
  } else {
    Serial.print("[ESP] Failed, rc=");
    Serial.print(client.state());
    Serial.println(" will retry in 5 seconds");
  }
}

// --- Main Loop ---
void loop() {
  // Only execute telemetry logic if WiFi is connected AND we have an MQTT server IP
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
      if (now - lastMsg > 2000) {
        lastMsg = now;

        // Generate dummy data
        float randomWeight = 10.0 + (random(0, 4000) / 100.0); 
        Serial.printf("[ESP] Generated Weight: %.2f kg\n", randomWeight);
        unsigned long timestamp = get_epoch_time();

        // Build JSON payload
        JsonDocument doc;
        doc["deviceId"] = deviceId;
        doc["weight"] = randomWeight;
        doc["timestamp"] = timestamp;

        char jsonBuffer[256];
        serializeJson(doc, jsonBuffer);
        
        Serial.print("[ESP] Publishing: ");
        Serial.println(jsonBuffer);
        client.publish(publish_topic, jsonBuffer);
      }
    }
  }
}