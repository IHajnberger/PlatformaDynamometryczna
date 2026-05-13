#include <stdio.h>
#include "freertos/FreeRTOS.h"
#include "freertos/task.h"
#include "driver/gpio.h"
#include "esp_log.h"
#include "rom/ets_sys.h" // dla ets_delay_us
#include "esp_timer.h"

static const char *TAG = "WAGI";

#define DOUT1_PIN 3
#define SCK1_PIN 4
#define DOUT2_PIN 5
#define SCK2_PIN 6

// Przykładowe współczynniki kalibracji (wymagają dostosowania do tensometrów)
#define SCALE_WAGA1 22800.0f
#define SCALE_WAGA2 22800.0f

void hx711_init(gpio_num_t dout, gpio_num_t sck) {
    gpio_reset_pin(dout);
    gpio_set_direction(dout, GPIO_MODE_INPUT);

    gpio_reset_pin(sck);
    gpio_set_direction(sck, GPIO_MODE_OUTPUT);
    gpio_set_level(sck, 0);
}

int32_t hx711_read(gpio_num_t dout, gpio_num_t sck) {
    // Oczekiwanie na gotowość (DOUT w stanie niskim)
    while (gpio_get_level(dout)) {
        vTaskDelay(1 / portTICK_PERIOD_MS);
    }

    int32_t count = 0;
    for (int i = 0; i < 24; i++) {
        gpio_set_level(sck, 1);
        ets_delay_us(1);
        count = count << 1;
        gpio_set_level(sck, 0);
        ets_delay_us(1);
        if (gpio_get_level(dout)) {
            count++;
        }
    }

    // 25-ty puls dla wzmocnienia = 128 (Kanał A)
    gpio_set_level(sck, 1);
    ets_delay_us(1);
    gpio_set_level(sck, 0);
    ets_delay_us(1);

    // Rozszerzenie znaku (z 24-bitów na 32-bity ze znakiem)
    if (count & 0x800000) {
        count |= 0xFF000000;
    }
    return count;
}

void app_main(void) {
    hx711_init(DOUT1_PIN, SCK1_PIN);
    hx711_init(DOUT2_PIN, SCK2_PIN);

    ESP_LOGI(TAG, "Tarowanie wag (ustaw na plasko i nie dotykaj)...");
    int32_t sum1 = 0, sum2 = 0;
    for (int i = 0; i < 10; i++) {
        sum1 += hx711_read(DOUT1_PIN, SCK1_PIN);
        sum2 += hx711_read(DOUT2_PIN, SCK2_PIN);
        vTaskDelay(100 / portTICK_PERIOD_MS);
    }
    int32_t offset1 = sum1 / 10;
    int32_t offset2 = sum2 / 10;
    ESP_LOGI(TAG, "Tarowanie zakonczone.");

    while (1) {
        int32_t odczyt1 = hx711_read(DOUT1_PIN, SCK1_PIN);
        int32_t odczyt2 = hx711_read(DOUT2_PIN, SCK2_PIN);

        float waga1_kg = (odczyt1 - offset1) / SCALE_WAGA1;
        float waga2_kg = -(odczyt2 - offset2) / SCALE_WAGA2;

        int64_t timestamp_ms = esp_timer_get_time() / 1000;
        ESP_LOGI(TAG, "[%lld ms] Waga 1: %.3f kg | Waga 2: %.3f kg", timestamp_ms, waga1_kg, waga2_kg);
        
        vTaskDelay(1000 / portTICK_PERIOD_MS);
    }
}