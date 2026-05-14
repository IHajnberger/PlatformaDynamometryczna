/*
 * ESP32 Weight Scale Firmware
 *
 * Features:
 * - Dual HX711 reading.
 * - WiFi Configuration Mode: Receives SSID/Password over USB Serial.
 * - Standalone Operation Mode: Connects to WiFi and broadcasts sensor data via UDP.
 * - Persistent Credentials: Saves WiFi settings to Non-Volatile Storage (NVS).
 */

#include <stdio.h>
#include <string.h>
#include "freertos/FreeRTOS.h"
#include "freertos/task.h"
#include "driver/gpio.h"
#include "esp_log.h"
#include "rom/ets_sys.h"
#include "esp_timer.h"
#include "esp_event.h"
#include "esp_wifi.h"
#include "nvs_flash.h"
#include "lwip/err.h"
#include "lwip/sockets.h"
#include "lwip/sys.h"

// --- Pin Definitions & Calibration
static const char *TAG = "WAGI";
#define DOUT1_PIN 3
#define SCK1_PIN 4
#define DOUT2_PIN 5
#define SCK2_PIN 6
#define SCALE_WAGA1 22800.0f
#define SCALE_WAGA2 22800.0f

// --- WiFi & Network Definitions
#define NVS_NAMESPACE "wifi_creds"
#define UDP_BROADCAST_PORT 12345
#define WIFI_CONNECTED_BIT BIT0
#define WIFI_FAIL_BIT BIT1
static EventGroupHandle_t s_wifi_event_group;
static int s_retry_num = 0;

// --- HX711 Functions (unchanged)
void hx711_init(gpio_num_t dout, gpio_num_t sck)
{
    gpio_reset_pin(dout);
    gpio_set_direction(dout, GPIO_MODE_INPUT);
    gpio_reset_pin(sck);
    gpio_set_direction(sck, GPIO_MODE_OUTPUT);
    gpio_set_level(sck, 0);
}

int32_t hx711_read(gpio_num_t dout, gpio_num_t sck)
{
    int timeout = 1000; // 1 second timeout
    while (gpio_get_level(dout))
    {
        vTaskDelay(1 / portTICK_PERIOD_MS);
        timeout--;
        if (timeout <= 0) {
            ESP_LOGE(TAG, "HX711 timeout on pin %d! Sensor disconnected?", dout);
            return 0; // Return 0 to prevent hanging
        }
    }
    int32_t count = 0;
    for (int i = 0; i < 24; i++)
    {
        gpio_set_level(sck, 1);
        ets_delay_us(1);
        count = count << 1;
        gpio_set_level(sck, 0);
        ets_delay_us(1);
        if (gpio_get_level(dout))
        {
            count++;
        }
    }
    gpio_set_level(sck, 1);
    ets_delay_us(1);
    gpio_set_level(sck, 0);
    ets_delay_us(1);
    if (count & 0x800000)
    {
        count |= 0xFF000000;
    }
    return count;
}

// --- NVS Functions
esp_err_t save_wifi_creds(const char *ssid, const char *password)
{
    nvs_handle_t my_handle;
    esp_err_t err = nvs_open(NVS_NAMESPACE, NVS_READWRITE, &my_handle);
    if (err != ESP_OK)
        return err;
    err = nvs_set_str(my_handle, "ssid", ssid);
    if (err == ESP_OK)
        err = nvs_set_str(my_handle, "password", password);
    if (err == ESP_OK)
        err = nvs_commit(my_handle);
    nvs_close(my_handle);
    return err;
}

esp_err_t load_wifi_creds(char *ssid, size_t max_ssid_len, char *password, size_t max_pass_len)
{
    nvs_handle_t my_handle;
    esp_err_t err = nvs_open(NVS_NAMESPACE, NVS_READONLY, &my_handle);
    if (err != ESP_OK)
        return err;
    err = nvs_get_str(my_handle, "ssid", ssid, &max_ssid_len);
    if (err == ESP_OK)
        err = nvs_get_str(my_handle, "password", password, &max_pass_len);
    nvs_close(my_handle);
    return err;
}

// --- WiFi Event Handler
static void event_handler(void *arg, esp_event_base_t event_base, int32_t event_id, void *event_data)
{
    if (event_base == WIFI_EVENT && event_id == WIFI_EVENT_STA_START)
    {
        esp_wifi_connect();
    }
    else if (event_base == WIFI_EVENT && event_id == WIFI_EVENT_STA_DISCONNECTED)
    {
        if (s_retry_num < 5)
        {
            esp_wifi_connect();
            s_retry_num++;
            ESP_LOGI(TAG, "retrying to connect to the AP");
        }
        else
        {
            xEventGroupSetBits(s_wifi_event_group, WIFI_FAIL_BIT);
        }
    }
    else if (event_base == IP_EVENT && event_id == IP_EVENT_STA_GOT_IP)
    {
        ip_event_got_ip_t *event = (ip_event_got_ip_t *)event_data;
        ESP_LOGI(TAG, "got ip:" IPSTR, IP2STR(&event->ip_info.ip));
        s_retry_num = 0;
        xEventGroupSetBits(s_wifi_event_group, WIFI_CONNECTED_BIT);
    }
}

// --- Main WiFi Logic
void wifi_init_sta(const char *ssid, const char *password)
{
    s_wifi_event_group = xEventGroupCreate();
    ESP_ERROR_CHECK(esp_netif_init());
    ESP_ERROR_CHECK(esp_event_loop_create_default());
    esp_netif_create_default_wifi_sta();

    wifi_init_config_t cfg = WIFI_INIT_CONFIG_DEFAULT();
    ESP_ERROR_CHECK(esp_wifi_init(&cfg));

    esp_event_handler_instance_t instance_any_id;
    esp_event_handler_instance_t instance_got_ip;
    ESP_ERROR_CHECK(esp_event_handler_instance_register(WIFI_EVENT, ESP_EVENT_ANY_ID, &event_handler, NULL, &instance_any_id));
    ESP_ERROR_CHECK(esp_event_handler_instance_register(IP_EVENT, IP_EVENT_STA_GOT_IP, &event_handler, NULL, &instance_got_ip));

    wifi_config_t wifi_config = {};
    strcpy((char *)wifi_config.sta.ssid, ssid);
    strcpy((char *)wifi_config.sta.password, password);
    wifi_config.sta.threshold.authmode = WIFI_AUTH_WPA2_PSK;

    ESP_ERROR_CHECK(esp_wifi_set_mode(WIFI_MODE_STA));
    ESP_ERROR_CHECK(esp_wifi_set_config(WIFI_IF_STA, &wifi_config));
    ESP_ERROR_CHECK(esp_wifi_start());

    EventBits_t bits = xEventGroupWaitBits(s_wifi_event_group, WIFI_CONNECTED_BIT | WIFI_FAIL_BIT, pdFALSE, pdFALSE, portMAX_DELAY);

    if (bits & WIFI_CONNECTED_BIT)
    {
        ESP_LOGI(TAG, "Connected to AP SSID:%s", ssid);
    }
    else if (bits & WIFI_FAIL_BIT)
    {
        ESP_LOGW(TAG, "Failed to connect to SSID:%s", ssid);
    }
    else
    {
        ESP_LOGE(TAG, "UNEXPECTED EVENT");
    }
}

// --- Task to handle UDP broadcasting
void udp_broadcast_task(void *pvParameters)
{
    char payload[100];
    int32_t offset1 = *((int32_t *)pvParameters);
    int32_t offset2 = *(((int32_t *)pvParameters) + 1);

    int sock = socket(AF_INET, SOCK_DGRAM, IPPROTO_IP);
    if (sock < 0)
    {
        ESP_LOGE(TAG, "Unable to create socket: errno %d", errno);
        vTaskDelete(NULL);
        return;
    }

    struct sockaddr_in dest_addr;
    dest_addr.sin_addr.s_addr = inet_addr("255.255.255.255"); // Broadcast
    dest_addr.sin_family = AF_INET;
    dest_addr.sin_port = htons(UDP_BROADCAST_PORT);

    int broadcast = 1;
    if (setsockopt(sock, SOL_SOCKET, SO_BROADCAST, &broadcast, sizeof(broadcast)) < 0)
    {
        ESP_LOGE(TAG, "Failed to set broadcast option: errno %d", errno);
        close(sock);
        vTaskDelete(NULL);
        return;
    }

    ESP_LOGI(TAG, "Starting UDP broadcast of weight data...");
    while (1)
    {

        int32_t odczyt1 = hx711_read(DOUT1_PIN, SCK1_PIN);
        int32_t odczyt2 = hx711_read(DOUT2_PIN, SCK2_PIN);

        float waga1_kg = (odczyt1 - offset1) / SCALE_WAGA1;
        float waga2_kg = -(odczyt2 - offset2) / SCALE_WAGA2;
        int64_t timestamp_ms = esp_timer_get_time() / 1000;

        // Format: timestamp,weight1,weight2
        snprintf(payload, sizeof(payload), "%lld,%.3f,%.3f", timestamp_ms, waga1_kg, waga2_kg);

        int err = sendto(sock, payload, strlen(payload), 0, (struct sockaddr *)&dest_addr, sizeof(dest_addr));
        if (err < 0)
        {
            ESP_LOGE(TAG, "Error sending data: errno %d", errno);
        }

        vTaskDelay(1000 / portTICK_PERIOD_MS);
    }
}

// --- Task to handle serial configuration
// --- Task to handle serial configuration
void serial_config_task(void *pvParameters) {
    char buffer[256];
    int idx = 0;
    ESP_LOGI(TAG, "Configuration mode active.");

    while (1) {
        int c = fgetc(stdin); // Read one character at a time
        if (c != EOF) {
            if (c == '\n' || c == '\r') {
                if (idx > 0) {
                    buffer[idx] = 0;
                    ESP_LOGI(TAG, "I heard: '%s'", buffer);

                    if (strncmp(buffer, "PING", 4) == 0) {
                        printf("START_APLIKACJA\n");
                        fflush(stdout);
                    } else if (strncmp(buffer, "WIFI_CONFIG:", 12) == 0) {
                        char *ssid = strtok(buffer + 12, ":");
                        char *password = strtok(NULL, ":");
                        if (ssid && password) {
                            
                            // FIX: Actually save the credentials to NVS
                            save_wifi_creds(ssid, password);
                            
                            printf("WIFI_CONFIRMED\n");
                            fflush(stdout);
                            vTaskDelay(1000 / portTICK_PERIOD_MS);
                            esp_restart();
                        }
                    }
                    idx = 0; // Reset for next command
                }
            } else if (idx < sizeof(buffer) - 1) {
                buffer[idx++] = (char)c;
            }
        } else {
            vTaskDelay(10 / portTICK_PERIOD_MS); // Wait if no data
        }
    }
}

void app_main(void)
{
    // Initialize NVS
    esp_err_t ret = nvs_flash_init();
    if (ret == ESP_ERR_NVS_NO_FREE_PAGES || ret == ESP_ERR_NVS_NEW_VERSION_FOUND)
    {
        ESP_ERROR_CHECK(nvs_flash_erase());
        ret = nvs_flash_init();
    }
    ESP_ERROR_CHECK(ret);

    // Give USB time to initialize, but DO NOT send the handshake here.
    vTaskDelay(1000 / portTICK_PERIOD_MS); 

    // Initialize HX711 sensors
    hx711_init(DOUT1_PIN, SCK1_PIN);
    hx711_init(DOUT2_PIN, SCK2_PIN);

    ESP_LOGI(TAG, "Taring scales...");
    int32_t sum1 = 0, sum2 = 0;
    for (int i = 0; i < 10; i++)
    {
        sum1 += hx711_read(DOUT1_PIN, SCK1_PIN);
        sum2 += hx711_read(DOUT2_PIN, SCK2_PIN);
        vTaskDelay(100 / portTICK_PERIOD_MS);
    }
    static int32_t offsets[2];
    offsets[0] = sum1 / 10;
    offsets[1] = sum2 / 10;
    ESP_LOGI(TAG, "Taring complete.");

    // Try to load WiFi credentials
    char ssid[64] = {0};
    char password[64] = {0};
    if (load_wifi_creds(ssid, sizeof(ssid), password, sizeof(password)) == ESP_OK && strlen(ssid) > 0)
    {
        ESP_LOGI(TAG, "Found credentials for SSID: %s. Starting WiFi...", ssid);
        wifi_init_sta(ssid, password);

        // Check if WiFi connected
        EventBits_t bits = xEventGroupGetBits(s_wifi_event_group);
        if (bits & WIFI_CONNECTED_BIT)
        {
            // Start UDP task if connected
            xTaskCreate(udp_broadcast_task, "udp_broadcast_task", 4096, offsets, 5, NULL);
        }
        else
        {
            // Fallback to config mode if connection fails
            ESP_LOGW(TAG, "WiFi connection failed. Falling back to configuration mode.");
        }
    }
    else    
    {
        ESP_LOGI(TAG, "No WiFi credentials found. Starting configuration mode.");
    }   

    // ALWAYS start the serial config task so it responds to PING and stays configurable!
    xTaskCreate(serial_config_task, "serial_config_task", 4096, NULL, 5, NULL);
}