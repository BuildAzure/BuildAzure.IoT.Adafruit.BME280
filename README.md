# BuildAzure.IoT.Adafruit.BME280
An [Adafruit BME280 (Pressure, Temp &amp; Humidity) Sensor](https://learn.adafruit.com/adafruit-bme280-humidity-barometric-pressure-temperature-sensor-breakout) library for Windows IoT Core

### Nuget Package
[![BuildAzure.IoT.Adafruit.BME280 Nuget Package](NugetCommand.png)](https://www.nuget.org/packages/BuildAzure.IoT.Adafruit.BME280)

### Wiring Diagram
Here's a simple Fritzing diagram that shows the expected wiring of the [Adafruit BME280 sensor](https://learn.adafruit.com/adafruit-bme280-humidity-barometric-pressure-temperature-sensor-breakout) with a Raspberry Pi 2 or 3:

![BMD280 Raspberry Pi Wiring Diagram](BME280Fritzing.png)

### Origins
This code was originally posted as part of the [Weather Station V 2.0 project](https://www.hackster.io/windows-iot/weather-station-v-2-0-8abe16) on [hackster.io](http://hackster.io). Since that project wasn't released using any reusable Nuget libraries for working with the Adafruit BME280 sensor, this project was created to borrow the BME280 code and release that out as a Nuget package for others to consume. Thankfully it's all licensed under the MIT License, and so is this project!
