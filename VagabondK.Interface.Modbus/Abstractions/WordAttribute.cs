using System;
using System.Reflection;
using VagabondK.Interface.Abstractions;
using VagabondK.Protocols.Modbus;

namespace VagabondK.Interface.Modbus.Abstractions
{
    public abstract class WordAttribute : ModbusBindingAttribute
    {
        protected WordAttribute(ushort address) : base(address) { }
        protected WordAttribute(byte slaveAddress, ushort address) : base(slaveAddress, address) { }
        protected WordAttribute(ushort address, ushort requestAddress, ushort requestLength) : base(address, requestAddress, requestLength) { }
        protected WordAttribute(byte slaveAddress, ushort address, ushort requestAddress, ushort requestLength) : base(slaveAddress, address, requestAddress, requestLength) { }

        public Type Type { get; set; }
        public int BytesLength { get; set; }
        public bool SkipFirstByte { get; set; }
        public double Scale { get; set; } = 1;
        public ModbusEndian Endian { get; set; } = ModbusEndian.AllBig;
        public byte BitIndex { get; set; }
        public string Encoding { get; set; } = System.Text.Encoding.UTF8.WebName;
        public DateTimeFormat DateTimeFormat { get; set; } = DateTimeFormat.UnixTime;
        public DateTimeKind DateTimeKind { get; set; } = DateTimeKind.Utc;
        public int TicksScalePowerOf10 { get; set; }
        public string DateTimeFormatString { get; set; }

        internal protected InterfacePoint OnCreatePoint(MemberInfo memberInfo, InterfaceAttribute rootAttribute, bool writable, bool? useMultiWriteFunction = null)
        {
            Type memberType;
            if (memberInfo is PropertyInfo property)
                memberType = property.PropertyType;
            else if (memberInfo is FieldInfo field)
                memberType = field.FieldType;
            else return null;

            var typeName = (Type ?? memberType).Name;
            switch (typeName)
            {
                case nameof(Boolean):
                    return new BitFlagPoint(GetSlaveAddress(rootAttribute), writable, Address, BitIndex, Endian, RequestAddress, RequestLength, useMultiWriteFunction, null);
                case nameof(SByte):
                case nameof(Byte):
                case nameof(Int16):
                case nameof(UInt16):
                case nameof(Int32):
                case nameof(UInt32):
                case nameof(Int64):
                case nameof(UInt64):
                case nameof(Single):
                case nameof(Double):
                    var numericPointType = typeof(ModbusPoint).Assembly.GetType($"{nameof(VagabondK)}.{nameof(Interface)}.{nameof(Modbus)}.{typeName}Point`1");
                    if (numericPointType == null) return null;
                    var pointType = numericPointType.MakeGenericType(memberType);
                    return (InterfacePoint)Activator.CreateInstance(pointType, GetSlaveAddress(rootAttribute), writable, Address, Scale, SkipFirstByte, Endian, RequestAddress, RequestLength, useMultiWriteFunction, null);
                case nameof(DateTime):
                    switch (DateTimeFormat)
                    {
                        case DateTimeFormat.UnixTime:
                            return new UnixTimePoint(GetSlaveAddress(rootAttribute), writable, Address, TicksScalePowerOf10,
                                BytesLength <= 0 ? 8 : BytesLength, DateTimeKind, SkipFirstByte, Endian, RequestAddress, RequestLength, useMultiWriteFunction, null);
                        case DateTimeFormat.DotNet:
                            return new DotNetDateTimePoint(GetSlaveAddress(rootAttribute), writable, Address, SkipFirstByte, Endian, RequestAddress, RequestLength, useMultiWriteFunction, null);
                        case DateTimeFormat.Ticks:
                            return new TicksDateTimePoint(GetSlaveAddress(rootAttribute), writable, Address, TicksScalePowerOf10,
                                BytesLength <= 0 ? 8 : BytesLength, DateTimeKind, SkipFirstByte, Endian, RequestAddress, RequestLength, useMultiWriteFunction, null);
                        case DateTimeFormat.String:
                            return new StringDateTimePoint(GetSlaveAddress(rootAttribute), writable, Address, DateTimeFormatString, DateTimeKind,
                                System.Text.Encoding.GetEncoding(Encoding), SkipFirstByte, Endian, RequestAddress, RequestLength, useMultiWriteFunction, null);
                        case DateTimeFormat.Bytes:
                            return new BytesDateTimePoint(GetSlaveAddress(rootAttribute), writable, Address, DateTimeFormatString, DateTimeKind,
                                SkipFirstByte, Endian, RequestAddress, RequestLength, useMultiWriteFunction, null);
                        default:
                            return null;
                    }
                case "Byte[]":
                    return new ByteArrayPoint(GetSlaveAddress(rootAttribute), writable, Address,
                        BytesLength, SkipFirstByte, Endian, RequestAddress, RequestLength, useMultiWriteFunction, null);
                case nameof(String):
                    return new StringPoint(GetSlaveAddress(rootAttribute), writable, Address,
                        BytesLength, System.Text.Encoding.GetEncoding(Encoding), SkipFirstByte, Endian, RequestAddress, RequestLength, useMultiWriteFunction, null);
                default:
                    return null;
            };
        }
    }
}
