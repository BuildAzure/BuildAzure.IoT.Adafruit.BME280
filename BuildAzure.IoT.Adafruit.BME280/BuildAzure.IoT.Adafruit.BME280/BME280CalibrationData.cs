// MIT License
// Original Source: https://github.com/ms-iot/adafruitsample/tree/master/Lesson_203V2/FullSolution

using System;

namespace BuildAzure.IoT.Adafruit.BME280
{
    public class BME280_CalibrationData
    {
        //BME280 Registers
        public UInt16 dig_T1 { get; set; }
        public Int16 dig_T2 { get; set; }
        public Int16 dig_T3 { get; set; }

        public UInt16 dig_P1 { get; set; }
        public Int16 dig_P2 { get; set; }
        public Int16 dig_P3 { get; set; }
        public Int16 dig_P4 { get; set; }
        public Int16 dig_P5 { get; set; }
        public Int16 dig_P6 { get; set; }
        public Int16 dig_P7 { get; set; }
        public Int16 dig_P8 { get; set; }
        public Int16 dig_P9 { get; set; }

        public byte dig_H1 { get; set; }
        public Int16 dig_H2 { get; set; }
        public byte dig_H3 { get; set; }
        public Int16 dig_H4 { get; set; }
        public Int16 dig_H5 { get; set; }
        public SByte dig_H6 { get; set; }

    }
}
