// MIT License
// Original Source: https://github.com/ms-iot/adafruitsample/tree/master/Lesson_203V2/FullSolution

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.I2c;

namespace BuildAzure.IoT.Adafruit.BME280
{
    // Sampling rates
    public enum SensorSampling : uint
    {
        SAMPLING_NONE = 0b000,
        SAMPLING_X1 = 0b001,
        SAMPLING_X2 = 0b010,
        SAMPLING_X4 = 0b011,
        SAMPLING_X8 = 0b100,
        SAMPLING_X16 = 0b101
    };

    // Power modes
    public enum SensorMode : uint
    {
        MODE_SLEEP = 0b00,
        MODE_FORCED = 0b01,
        MODE_NORMAL = 0b11
    };

    // Filter values
    public enum SensorFilter : uint
    {
        FILTER_OFF = 0b000,
        FILTER_X2 = 0b001,
        FILTER_X4 = 0b010,
        FILTER_X8 = 0b011,
        FILTER_X16 = 0b100
    };

    // Standby duration in ms
    public enum StandbyDuration : uint
    {
        STANDBY_MS_0_5 = 0b000,
        STANDBY_MS_10 = 0b110,
        STANDBY_MS_20 = 0b111,
        STANDBY_MS_62_5 = 0b001,
        STANDBY_MS_125 = 0b010,
        STANDBY_MS_250 = 0b011,
        STANDBY_MS_500 = 0b100,
        STANDBY_MS_1000 = 0b101
    };

    public class BME280Sensor
    {
        //The BME280 register addresses according the the datasheet: http://www.adafruit.com/datasheets/BST-BME280-DS001-11.pdf
        const byte BME280_Address = 0x77;
        const byte BME280_Signature = 0x60;

        enum eRegisters : byte
        {
            BME280_REGISTER_DIG_T1 = 0x88,
            BME280_REGISTER_DIG_T2 = 0x8A,
            BME280_REGISTER_DIG_T3 = 0x8C,

            BME280_REGISTER_DIG_P1 = 0x8E,
            BME280_REGISTER_DIG_P2 = 0x90,
            BME280_REGISTER_DIG_P3 = 0x92,
            BME280_REGISTER_DIG_P4 = 0x94,
            BME280_REGISTER_DIG_P5 = 0x96,
            BME280_REGISTER_DIG_P6 = 0x98,
            BME280_REGISTER_DIG_P7 = 0x9A,
            BME280_REGISTER_DIG_P8 = 0x9C,
            BME280_REGISTER_DIG_P9 = 0x9E,

            BME280_REGISTER_DIG_H1 = 0xA1,
            BME280_REGISTER_DIG_H2 = 0xE1,
            BME280_REGISTER_DIG_H3 = 0xE3,
            BME280_REGISTER_DIG_H4 = 0xE4,
            BME280_REGISTER_DIG_H5 = 0xE5,
            BME280_REGISTER_DIG_H6 = 0xE7,

            BME280_REGISTER_CHIPID = 0xD0,
            BME280_REGISTER_VERSION = 0xD1,
            BME280_REGISTER_SOFTRESET = 0xE0,

            BME280_REGISTER_CAL26 = 0xE1,  // R calibration stored in 0xE1-0xF0

            BME280_REGISTER_CONTROLHUMID = 0xF2,
            BME280_REGISTER_STATUS = 0XF3,
            BME280_REGISTER_CONTROL = 0xF4,
            BME280_REGISTER_CONFIG = 0xF5,

            BME280_REGISTER_PRESSUREDATA_MSB = 0xF7,
            BME280_REGISTER_PRESSUREDATA_LSB = 0xF8,
            BME280_REGISTER_PRESSUREDATA_XLSB = 0xF9, // bits <7:4>

            BME280_REGISTER_TEMPDATA_MSB = 0xFA,
            BME280_REGISTER_TEMPDATA_LSB = 0xFB,
            BME280_REGISTER_TEMPDATA_XLSB = 0xFC, // bits <7:4>

            BME280_REGISTER_HUMIDDATA_MSB = 0xFD,
            BME280_REGISTER_HUMIDDATA_LSB = 0xFE,
        };

        // The config register
        struct Config
        {
            // inactive duration (standby time) in normal mode
            // 000 = 0.5 ms
            // 001 = 62.5 ms
            // 010 = 125 ms
            // 011 = 250 ms
            // 100 = 500 ms
            // 101 = 1000 ms
            // 110 = 10 ms
            // 111 = 20 ms
            public uint t_sb; //3bits

            // filter settings
            // 000 = filter off
            // 001 = 2x filter
            // 010 = 4x filter
            // 011 = 8x filter
            // 100 and above = 16x filter
            public uint filter; //3bits

            public uint spi3w_en; //1bit

            public uint Get()
            {
                return ((t_sb&0x7) << 5) | ((filter&0x7) << 2) | (spi3w_en&0x1);
            }
        };
        Config _configReg;


        // The ctrl_meas register
        struct CtrlMeas
        {
            // temperature oversampling
            // 000 = skipped
            // 001 = x1
            // 010 = x2
            // 011 = x4
            // 100 = x8
            // 101 and above = x16
            public uint osrs_t;// 3bits;

            // pressure oversampling
            // 000 = skipped
            // 001 = x1
            // 010 = x2
            // 011 = x4
            // 100 = x8
            // 101 and above = x16
            public uint osrs_p; //3bits;

            // device mode
            // 00       = sleep
            // 01 or 10 = forced
            // 11       = normal
            public uint mode; //2bits;

            public uint Get()
            {
                return ((osrs_t&0x7) << 5) | ((osrs_p&0x7) << 2) | (mode&0x3);
            }
        };
        CtrlMeas _measReg;


        // The ctrl_hum register
        struct CtrlHum
        {
             // pressure oversampling
            // 000 = skipped
            // 001 = x1
            // 010 = x2
            // 011 = x4
            // 100 = x8
            // 101 and above = x16
            public uint osrs_h; //3bits;

            public uint Get()
            {
                return osrs_h&0x7;
            }
        };
        CtrlHum _humReg;


        //String for the friendly name of the I2C bus 
        const string I2CControllerName = "I2C1";
        //Create an I2C device
        private I2cDevice bme280 = null;
        //Create new calibration data for the sensor
        BME280_CalibrationData CalibrationData;
        //Variable to check if device is initialized
        bool init = false;

        public async Task<bool> Initialize()
        {
            return await Initialize(BME280_Address);
        }
        //Method to initialize the BME280 sensor
        public async Task<bool> Initialize(byte address)
        {
            Debug.WriteLine("BME280::Initialize");
            bool ok = false;
            try
            {
                //Instantiate the I2CConnectionSettings using the device address of the BME280
                I2cConnectionSettings settings = new I2cConnectionSettings(address);
                //Set the I2C bus speed of connection to fast mode
                settings.BusSpeed = I2cBusSpeed.FastMode;
                //Use the I2CBus device selector to create an advanced query syntax string
                string aqs = I2cDevice.GetDeviceSelector(I2CControllerName);
                //Use the Windows.Devices.Enumeration.DeviceInformation class to create a collection using the advanced query syntax string
                DeviceInformationCollection dis = await DeviceInformation.FindAllAsync(aqs);
                //Instantiate the the BME280 I2C device using the device id of the I2CBus and the I2CConnectionSettings
                bme280 = await I2cDevice.FromIdAsync(dis[0].Id, settings);
                //Check if device was found
                if (bme280 == null)
                {
                    Debug.WriteLine("Device not found");
                } else
                {
                    await Begin();
                    ok = true;
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("Exception: " + e.Message + "\n" + e.StackTrace);
                throw;
            }
            return ok;

        }
        private async Task Begin()
        {
            Debug.WriteLine("BME280::Begin");
            byte[] WriteBuffer = new byte[] { (byte)eRegisters.BME280_REGISTER_CHIPID };
            byte[] ReadBuffer = new byte[] { 0xFF };

            //Read the device signature
            bme280.WriteRead(WriteBuffer, ReadBuffer);
            Debug.WriteLine("BME280 Signature: " + ReadBuffer[0].ToString());

            //Verify the device signature
            if (ReadBuffer[0] != BME280_Signature)
            {
                Debug.WriteLine("BME280::Begin Signature Mismatch.");
                return;
            }

            //Set the initialize variable to true
            init = true;

            //Read the coefficients table
            CalibrationData = await ReadCoefficeints();

            //Set default mode
            await SetSampling(SensorMode.MODE_NORMAL,
                SensorSampling.SAMPLING_X16,
                SensorSampling.SAMPLING_X16,
                SensorSampling.SAMPLING_X16,
                SensorFilter.FILTER_OFF,
                StandbyDuration.STANDBY_MS_0_5
             );

        }

        //BME280 allows various modes. See chapter 3.5 "Recommended modes of operation" in the datasheet
        public async Task SetSampling(SensorMode mode, SensorSampling tempSampling, SensorSampling pressSampling,
                                       SensorSampling humSampling, SensorFilter filter, StandbyDuration duration)
        {
            Debug.WriteLine("BME280: Set sampling");
            _measReg.mode = (uint)mode;
            _measReg.osrs_t = (uint)tempSampling;
            _measReg.osrs_p = (uint)pressSampling;


            _humReg.osrs_h = (uint) humSampling;
            _configReg.filter = (uint) filter;
            _configReg.t_sb = (uint) duration;


            // you must make sure to also set REGISTER_CONTROL after setting the
            // CONTROLHUMID register, otherwise the values won't be applied (see DS 5.4.3)
            byte[] WriteBuffer1 = new byte[] {
                (byte)eRegisters.BME280_REGISTER_CONTROLHUMID, (byte)_humReg.Get() };
            byte[] WriteBuffer2 = new byte[] {
                (byte)eRegisters.BME280_REGISTER_CONFIG, (byte) _configReg.Get() };
            byte[] WriteBuffer3 = new byte[] {
                (byte)eRegisters.BME280_REGISTER_CONTROL, (byte) _measReg.Get()
            };
            bme280.Write(WriteBuffer1);
            bme280.Write(WriteBuffer2);
            bme280.Write(WriteBuffer3);

            await Task.Delay(1);
        }

        public async Task TakeForcedMeasurement()
        {
            // If we are in forced mode, the BME sensor goes back to sleep after each
            // measurement and we need to set it to forced mode once at this point, so
            // it will take the next measurement and then return to sleep again.
            // In normal mode simply does new measurements periodically.
            if (_measReg.mode == (uint)SensorMode.MODE_FORCED)
            {
                // set to forced mode, i.e. "take next measurement"
                byte[] WriteBuffer = new byte[] { (byte)eRegisters.BME280_REGISTER_CONTROL, (byte)_measReg.Get() };
                bme280.Write(WriteBuffer);
                // wait until measurement has been completed, otherwise we would read
                // the values from the last measurement
                while ((ReadByte((byte)eRegisters.BME280_REGISTER_STATUS) & 0x08) != 0)
                {
                    await Task.Delay(1);
                }
            }
        }



        //Method to read a 16-bit value from a register and return it in little endian format
        private UInt16 ReadUInt16_LittleEndian(byte register)
        {
            UInt16 value = 0;
            byte[] writeBuffer = new byte[] { 0x00 };
            byte[] readBuffer = new byte[] { 0x00, 0x00 };

            writeBuffer[0] = register;

            bme280.WriteRead(writeBuffer, readBuffer);
            int h = readBuffer[1] << 8;
            int l = readBuffer[0];
            value = (UInt16)(h + l);
            return value;
        }

        //Method to read an 8-bit value from a register
        private byte ReadByte(byte register)
        {
            byte value = 0;
            byte[] writeBuffer = new byte[] { 0x00 };
            byte[] readBuffer = new byte[] { 0x00 };

            writeBuffer[0] = register;

            bme280.WriteRead(writeBuffer, readBuffer);
            value = readBuffer[0];
            return value;
        }

        //Method to read the calibration data from the registers
        private async Task<BME280_CalibrationData> ReadCoefficeints()
        {
            // 16 bit calibration data is stored as Little Endian, the helper method will do the byte swap.
            CalibrationData = new BME280_CalibrationData();

            // Read temperature calibration data
            CalibrationData.dig_T1 = ReadUInt16_LittleEndian((byte)eRegisters.BME280_REGISTER_DIG_T1);
            CalibrationData.dig_T2 = (Int16)ReadUInt16_LittleEndian((byte)eRegisters.BME280_REGISTER_DIG_T2);
            CalibrationData.dig_T3 = (Int16)ReadUInt16_LittleEndian((byte)eRegisters.BME280_REGISTER_DIG_T3);

            // Read presure calibration data
            CalibrationData.dig_P1 = ReadUInt16_LittleEndian((byte)eRegisters.BME280_REGISTER_DIG_P1);
            CalibrationData.dig_P2 = (Int16)ReadUInt16_LittleEndian((byte)eRegisters.BME280_REGISTER_DIG_P2);
            CalibrationData.dig_P3 = (Int16)ReadUInt16_LittleEndian((byte)eRegisters.BME280_REGISTER_DIG_P3);
            CalibrationData.dig_P4 = (Int16)ReadUInt16_LittleEndian((byte)eRegisters.BME280_REGISTER_DIG_P4);
            CalibrationData.dig_P5 = (Int16)ReadUInt16_LittleEndian((byte)eRegisters.BME280_REGISTER_DIG_P5);
            CalibrationData.dig_P6 = (Int16)ReadUInt16_LittleEndian((byte)eRegisters.BME280_REGISTER_DIG_P6);
            CalibrationData.dig_P7 = (Int16)ReadUInt16_LittleEndian((byte)eRegisters.BME280_REGISTER_DIG_P7);
            CalibrationData.dig_P8 = (Int16)ReadUInt16_LittleEndian((byte)eRegisters.BME280_REGISTER_DIG_P8);
            CalibrationData.dig_P9 = (Int16)ReadUInt16_LittleEndian((byte)eRegisters.BME280_REGISTER_DIG_P9);

            // Read humidity calibration data
            CalibrationData.dig_H1 = ReadByte((byte)eRegisters.BME280_REGISTER_DIG_H1);
            CalibrationData.dig_H2 = (Int16)ReadUInt16_LittleEndian((byte)eRegisters.BME280_REGISTER_DIG_H2);
            CalibrationData.dig_H3 = ReadByte((byte)eRegisters.BME280_REGISTER_DIG_H3);
            CalibrationData.dig_H4 = (Int16)((ReadByte((byte)eRegisters.BME280_REGISTER_DIG_H4) << 4) | (ReadByte((byte)eRegisters.BME280_REGISTER_DIG_H4 + 1) & 0xF));
            CalibrationData.dig_H5 = (Int16)((ReadByte((byte)eRegisters.BME280_REGISTER_DIG_H5 + 1) << 4) | (ReadByte((byte)eRegisters.BME280_REGISTER_DIG_H5) >> 4));
            CalibrationData.dig_H6 = (sbyte)ReadByte((byte)eRegisters.BME280_REGISTER_DIG_H6);

            await Task.Delay(1);
            return CalibrationData;
        }


        //t_fine carries fine temperature as global value
        Int32 t_fine = Int32.MinValue;
        //Method to return the temperature in DegC. Resolution is 0.01 DegC. Output value of “5123” equals 51.23 DegC.
        private double BME280_compensate_T_double(Int32 adc_T)
        {
            double var1, var2, T;

            //The temperature is calculated using the compensation formula in the BME280 datasheet
            var1 = ((adc_T / 16384.0) - (CalibrationData.dig_T1 / 1024.0)) * CalibrationData.dig_T2;
            var2 = ((adc_T / 131072.0) - (CalibrationData.dig_T1 / 8192.0)) * CalibrationData.dig_T3;

            t_fine = (Int32)(var1 + var2);

            T = (var1 + var2) / 5120.0;
            return T;
        }


        //Method to returns the pressure in Pa, in Q24.8 format (24 integer bits and 8 fractional bits).
        //Output value of “24674867” represents 24674867/256 = 96386.2 Pa = 963.862 hPa
        private Int64 BME280_compensate_P_Int64(Int32 adc_P)
        {
            Int64 var1, var2, p;

            //The pressure is calculated using the compensation formula in the BME280 datasheet
            var1 = t_fine - 128000;
            var2 = var1 * var1 * (Int64)CalibrationData.dig_P6;
            var2 = var2 + ((var1 * (Int64)CalibrationData.dig_P5) << 17);
            var2 = var2 + ((Int64)CalibrationData.dig_P4 << 35);
            var1 = ((var1 * var1 * (Int64)CalibrationData.dig_P3) >> 8) + ((var1 * (Int64)CalibrationData.dig_P2) << 12);
            var1 = (((((Int64)1 << 47) + var1)) * (Int64)CalibrationData.dig_P1) >> 33;
            if (var1 == 0)
            {
                Debug.WriteLine("BME280_compensate_P_Int64 Jump out to avoid / 0");
                return 0; //Avoid exception caused by division by zero
            }
            //Perform calibration operations as per datasheet: http://www.adafruit.com/datasheets/BST-BME280-DS001-11.pdf
            p = 1048576 - adc_P;
            p = (((p << 31) - var2) * 3125) / var1;
            var1 = ((Int64)CalibrationData.dig_P9 * (p >> 13) * (p >> 13)) >> 25;
            var2 = ((Int64)CalibrationData.dig_P8 * p) >> 19;
            p = ((p + var1 + var2) >> 8) + ((Int64)CalibrationData.dig_P7 << 4);
            return p;
        }

        // Returns humidity in %RH as unsigned 32 bit integer in Q22.10 format (22 integer and 10 fractional bits).
        // Output value of “47445” represents 47445/1024 = 46.333 %RH
        UInt32 bme280_compensate_H_int32(Int32 adc_H)
        {
            Int32 v_x1_u32r;
            v_x1_u32r = (t_fine - ((Int32)76800));
            v_x1_u32r = (((((adc_H << 14) - (((Int32)CalibrationData.dig_H4) << 20) - (((Int32)CalibrationData.dig_H5) * v_x1_u32r)) +
            ((Int32)16384)) >> 15) * (((((((v_x1_u32r * ((Int32)CalibrationData.dig_H6)) >> 10) * (((v_x1_u32r *
                ((Int32)CalibrationData.dig_H3)) >> 11) + ((Int32)32768))) >> 10) + ((Int32)2097152)) *
            ((Int32)CalibrationData.dig_H2) + 8192) >> 14));
            v_x1_u32r = (v_x1_u32r - (((((v_x1_u32r >> 15) * (v_x1_u32r >> 15)) >> 7) * ((Int32)CalibrationData.dig_H1)) >> 4));
            v_x1_u32r = (v_x1_u32r < 0 ? 0 : v_x1_u32r);
            v_x1_u32r = (v_x1_u32r > 419430400 ? 419430400 : v_x1_u32r);
            return (UInt32)(v_x1_u32r >> 12);
        }

        public async Task<float> ReadTemperature()
        {
            //Make sure the I2C device is initialized
            if (!init) await Begin();

            //Read the MSB, LSB and bits 7:4 (XLSB) of the temperature from the BME280 registers
            byte tmsb = ReadByte((byte)eRegisters.BME280_REGISTER_TEMPDATA_MSB);
            byte tlsb = ReadByte((byte)eRegisters.BME280_REGISTER_TEMPDATA_LSB);
            byte txlsb = ReadByte((byte)eRegisters.BME280_REGISTER_TEMPDATA_XLSB); // bits 7:4

            //Combine the values into a 32-bit integer
            Int32 t = (tmsb << 12) + (tlsb << 4) + (txlsb >> 4);

            //Convert the raw value to the temperature in degC
            double temp = BME280_compensate_T_double(t);

            //Return the temperature as a float value
            return (float)temp;
        }

        public async Task<float> ReadPressure()
        {
            //Make sure the I2C device is initialized
            if (!init) await Begin();

            //Read the temperature first to load the t_fine value for compensation
            if (t_fine == Int32.MinValue)
            {
                await ReadTemperature();
            }

            //Read the MSB, LSB and bits 7:4 (XLSB) of the pressure from the BME280 registers
            byte tmsb = ReadByte((byte)eRegisters.BME280_REGISTER_PRESSUREDATA_MSB);
            byte tlsb = ReadByte((byte)eRegisters.BME280_REGISTER_PRESSUREDATA_LSB);
            byte txlsb = ReadByte((byte)eRegisters.BME280_REGISTER_PRESSUREDATA_XLSB); // bits 7:4

            //Combine the values into a 32-bit integer
            Int32 t = (tmsb << 12) + (tlsb << 4) + (txlsb >> 4);

            //Convert the raw value to the pressure in Pa
            Int64 pres = BME280_compensate_P_Int64(t);

            //Return the temperature as a float value
            return ((float)pres) / 256;
        }

        public async Task<float> ReadHumidity()
        {
            if (!init) await Begin();

            byte tmsb = ReadByte((byte)eRegisters.BME280_REGISTER_HUMIDDATA_MSB);
            byte tlsb = ReadByte((byte)eRegisters.BME280_REGISTER_HUMIDDATA_LSB);
            Int32 uncompensated = (tmsb << 8) + tlsb;
            UInt32 humidity = bme280_compensate_H_int32(uncompensated);

            return ((float)humidity) / 1000;
        }

        //Method to take the sea level pressure in Hectopascals(hPa) as a parameter and calculate the altitude using current pressure.
        public async Task<float> ReadAltitude(float seaLevel)
        {
            //Make sure the I2C device is initialized
            if (!init) await Begin();

            //Read the pressure first
            float pressure = await ReadPressure();
            //Convert the pressure to Hectopascals(hPa)
            pressure /= 100;

            //Calculate and return the altitude using the international barometric formula
            return 44330.0f * (1.0f - (float)Math.Pow((pressure / seaLevel), 0.1903f));
        }

        // Calculates the pressure at sea level (in hPa) from the specified altitude (in meters) and atmospheric pressure (in hPa).  
        public float SeaLevelForAltitude(float altitude, float atmospheric)
        {
            // Equation taken from BMP180 datasheet (page 17):
            // https://ae-bst.resource.bosch.com/media/_tech/media/datasheets/BST-BMP180-DS000.pdf

            return atmospheric / (float)Math.Pow(1.0 - (altitude / 44330.0f), 5.255f);
        }

    }
}
