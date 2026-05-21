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
String mqtt_server = ""; 

const char* publish_topic = "esp32/scale/telemetry";
const char* ntpServer = "pool.ntp.org";
const long  gmtOffset_sec = 0;
const int   daylightOffset_sec = 0;

WiFiClient espClient;
PubSubClient client(espClient);
unsigned long lastMsg = 0;
unsigned long lastReconnectAttempt = 0;

// UNIQUE IDENTIFIER FOR THIS SPECIFIC ESP32
char deviceId[32] = "Right"; 

String inputBuffer = ""; // Globalny bufor dla komend z USB

// --- Function Prototypes ---
void sync_time();
void reconnect();
void check_serial_commands(); 
bool testWifiConnection(String testSsid, String testPass);

void setup() {
  // Inicjalizacja klasycznego portu szeregowego
  Serial.begin(115200);
  
  // Czekamy maksymalnie 2 sekundy na zainicjalizowanie portu w systemie Windows
  int usbTimeout = 0;
  while (!Serial && usbTimeout < 20) {
      delay(100);
      usbTimeout++;
  }

  // Odczyt danych z pamięci stałej NVS
  preferences.begin("wifi_creds", true); // true = read-only mode
  ssid = preferences.getString("ssid", "");
  password = preferences.getString("password", "");
  mqtt_server = preferences.getString("mqtt_server", ""); 
  String savedId = preferences.getString("device_id", "Right");
  strlcpy(deviceId, savedId.c_str(), sizeof(deviceId));
  preferences.end();

  // Próba automatycznego połączenia z Wi-Fi po włączeniu zasilania
  if (ssid.length() > 0) {
    Serial.printf("\n[ESP-%s] Connecting to SSID: %s\n", deviceId, ssid.c_str());
    WiFi.begin(ssid.c_str(), password.c_str());
    
    int attempts = 0;
    while (WiFi.status() != WL_CONNECTED && attempts < 20) {
      delay(500);
      Serial.print(".");
      attempts++;
    }

    if (WiFi.status() == WL_CONNECTED) {
      Serial.printf("\n[ESP-%s] WiFi Connected!\n", deviceId);
      Serial.printf("[ESP-%s] IP Address: ", deviceId);
      Serial.println(WiFi.localIP());
      
      sync_time();
      
      if (mqtt_server.length() > 0) {
        Serial.printf("[ESP-%s] Setting MQTT Broker to: %s\n", deviceId, mqtt_server.c_str());
        client.setServer(mqtt_server.c_str(), 1883);
      } else {
        Serial.printf("[ESP-%s] No MQTT Broker IP found. Waiting for configuration.\n", deviceId);
      }
    } else {
      Serial.printf("\n[ESP-%s] WiFi Connection Failed. Waiting for new configuration.\n", deviceId);
    }
  } else {
    Serial.printf("\n[ESP-%s] No WiFi credentials found. Waiting for configuration.\n", deviceId);
  }
}

// --- Main Loop ---
void loop() {
  // Sprawdzamy komendy z Avalonii przez Serial w każdym obiegu pętli głównej
  check_serial_commands();

  // Logika telemetryczna oraz utrzymanie połączenia z serwerem MQTT
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
      if (now - lastMsg >= 100) {
        lastMsg = now;

        // Generowanie fali danych na podstawie ID urządzenia
        float timeInSeconds = now / 1000.0;
        float generatedWeight = 0.0;
        if (strcmp(deviceId, "Left") == 0) {
          generatedWeight = sin(timeInSeconds);
        } else if (strcmp(deviceId, "Right") == 0) {
          generatedWeight = cos(timeInSeconds);
        } else {
          generatedWeight = 10.0 + (random(0, 20000) / 100.0); 
        }
        Serial.printf("[ESP-%s] Generated Weight: %.2f kg\n", deviceId, generatedWeight);

        // Formatowanie i wysyłanie czasu z milisekundami
        struct tm timeinfo;
        struct timeval tv;
        gettimeofday(&tv, NULL);
        localtime_r(&tv.tv_sec, &timeinfo);
        char timeStringBuff[50];
        strftime(timeStringBuff, sizeof(timeStringBuff), "%Y-%m-%d %H:%M:%S", &timeinfo);
        long milliseconds = tv.tv_usec / 1000;
        Serial.printf("[ESP-%s] Timestamp: %s.%03ld\n", deviceId, timeStringBuff, milliseconds);

        uint64_t timestamp = (uint64_t)tv.tv_sec;

        // Budowa paczki JSON
        JsonDocument doc;
        doc["deviceId"] = deviceId;
        doc["weight"] = generatedWeight;
        doc["timestamp"] = timestamp;

        char jsonBuffer[256];
        serializeJson(doc, jsonBuffer);
        
        Serial.printf("[ESP-%s] Publishing: %s\n", deviceId, jsonBuffer);
        client.publish(publish_topic, jsonBuffer);
      }
    }
  }
  
  // Niezbędne uśpienie dające czas stosowi USB na bezkonfliktową pracę
  vTaskDelay(1 / portTICK_PERIOD_MS);
}

// --- Obsługa Komend przez Serial ---
void check_serial_commands() {
  if (!Serial) return;

  while (Serial.available() > 0) {
    char c = Serial.read();
    
    if (c == '\n' || c == '\r') {
      inputBuffer.trim(); 
      
      if (inputBuffer.length() > 0) {
        
        // 1. Handshake
        if (inputBuffer == "PING") {
          Serial.println("START_APLIKACJA");
          Serial.flush();
        } 
        
        // 2. WiFi Configuration Command
        else if (inputBuffer.startsWith("WIFI_CONFIG:")) {
          int firstColon = inputBuffer.indexOf(':');
          int secondColon = inputBuffer.indexOf(':', firstColon + 1);

          if (firstColon > 0 && secondColon > firstColon) {
            String newSsid = inputBuffer.substring(firstColon + 1, secondColon);
            String newPass = inputBuffer.substring(secondColon + 1);

            Serial.printf("[ESP-%s] Testing new WiFi Config... SSID: %s\n", deviceId, newSsid.c_str());
            Serial.flush();

            if (testWifiConnection(newSsid, newPass)) {
              preferences.begin("wifi_creds", false);
              preferences.putString("ssid", newSsid);
              preferences.putString("password", newPass);
              preferences.end();

              Serial.printf("[ESP-%s] WIFI_CONFIRMED\n", deviceId);
              Serial.flush();
            } else {
              Serial.printf("[ESP-%s] WIFI_FAILED\n", deviceId);
              Serial.flush();
            }
          }
        }
        
        // 3. MQTT Configuration Command
        else if (inputBuffer.startsWith("MQTT_CONFIG:")) {
          String newMqtt = inputBuffer.substring(12); 
          newMqtt.trim();

          if (newMqtt.length() > 0) {
            Serial.printf("[ESP] Saving and applying new MQTT Broker... IP: %s\n", newMqtt.c_str());
            Serial.flush();

            preferences.begin("wifi_creds", false);
            preferences.putString("mqtt_server", newMqtt);
            preferences.end();

            mqtt_server = newMqtt;
            client.setServer(mqtt_server.c_str(), 1883);
            
            if (client.connected()) {
              client.disconnect();
            }

            Serial.println("[ESP] MQTT_CONFIRMED");
            Serial.flush();
          }
        }
        
        // 4. Device ID Configuration Command
        else if (inputBuffer.startsWith("DEVICE_ID_CONFIG:")) {
          String newDeviceId = inputBuffer.substring(17); 
          newDeviceId.trim();

          if (newDeviceId.length() > 0) {
            Serial.printf("[ESP] Saving new Device ID: %s\n", newDeviceId.c_str());
            Serial.flush();

            preferences.begin("wifi_creds", false);
            preferences.putString("device_id", newDeviceId);
            preferences.end();

            strlcpy(deviceId, newDeviceId.c_str(), sizeof(deviceId));
            Serial.println("[ESP] DEVICE_ID_CONFIRMED");
            Serial.flush();
          }
        }
        
        // 5. Bezpieczna komenda rozłączenia z wymuszonym restartem urządzenia
        else if (inputBuffer == "DISCONNECT_CMD") {
          Serial.println("[ESP] DISCONNECT_ACK");
          Serial.flush();
          
          if (client.connected()) {
            client.disconnect();
          }
          
          // Czyszczenie danych sieci z NVS, by po restarcie nie łączył się sam
          preferences.begin("wifi_creds", false);
          preferences.putString("ssid", "");
          preferences.putString("password", "");
          preferences.end();
          
          vTaskDelay(500 / portTICK_PERIOD_MS); // Czas na zapis i wysłanie ACK przez port COM
          
          // Restart oczyszcza gniazdo i zapobiega blokowaniu portu COM w Windows
          ESP.restart(); 
        }
        
        inputBuffer = ""; // Czyszczenie bufora
      }
    } else {
      inputBuffer += c; 
    }
  }
}

// --- Helper Functions ---
bool testWifiConnection(String testSsid, String testPass) {
  WiFi.disconnect();
  vTaskDelay(100 / portTICK_PERIOD_MS);

  WiFi.begin(testSsid.c_str(), testPass.c_str());

  int attempts = 0;
  while (WiFi.status() != WL_CONNECTED && attempts < 20) {
    vTaskDelay(500 / portTICK_PERIOD_MS); // Bezpieczne czekanie bez zamrażania USB
    attempts++;
  }
  return WiFi.status() == WL_CONNECTED;
}

void sync_time() {
  configTime(gmtOffset_sec, daylightOffset_sec, ntpServer);
  struct tm timeinfo;
  int timeout = 0;
  while (!getLocalTime(&timeinfo) && timeout < 20) {
    vTaskDelay(500 / portTICK_PERIOD_MS); 
    timeout++;
  }
}

void reconnect() {
  if (mqtt_server.length() == 0) return;
  String clientId = String(deviceId) + "-" + String(random(0xffff), HEX);
  client.connect(clientId.c_str());
}