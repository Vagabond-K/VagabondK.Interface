using System;
using System.Reflection;
using VagabondK.Interface.Abstractions;
using VagabondK.Protocols.Modbus;

namespace VagabondK.Interface.Modbus.Abstractions
{
    public abstract class ModbusRegisterAttribute : ModbusAttribute
    {
        protected ModbusRegisterAttribute(ushort address) : base(address) { }
        protected ModbusRegisterAttribute(byte slaveAddress, ushort address) : base(slaveAddress, address) { }
        protected ModbusRegisterAttribute(ushort address, ushort requestAddress, ushort requestLength) : base(address, requestAddress, requestLength) { }
        protected ModbusRegisterAttribute(byte slaveAddress, ushort address, ushort requestAddress, ushort requestLength) : base(slaveAddress, address, requestAddress, requestLength) { }

        public Type Type { get; set; }
        public int BytesLength { get; set; } = 2;
        public bool SkipFirstByte { get; set; }
        public double Scale { get; set; } = 1;
        public ModbusEndian Endian { get; set; } = ModbusEndian.AllBig;
        public byte BitIndex { get; set; }
        public string Encoding { get; set; } = System.Text.Encoding.UTF8.WebName;
        public bool UnixTimeIsMilliseconds { get; set; }
        public int UnixTimeScalePowerOf10 { get; set; }

        internal InterfacePoint OnCreatePoint(MemberInfo memberInfo, bool writable, bool? useMultiWriteFunction = null)
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
                    return new ModbusBitFlagPoint(GetSlaveAddress(memberInfo.ReflectedType), writable, Address, BitIndex, Endian, RequestAddress, RequestLength, useMultiWriteFunction, null);
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
                    var numericPointType = typeof(ModbusPoint).Assembly.GetType($"{nameof(VagabondK)}.{nameof(Interface)}.{nameof(Modbus)}.Modbus{typeName}Point`1");
                    if (numericPointType == null) return null;
                    var pointType = numericPointType.MakeGenericType(memberType);
                    return (InterfacePoint)Activator.CreateInstance(pointType, GetSlaveAddress(memberInfo.ReflectedType), writable, Address, Scale, SkipFirstByte, Endian, RequestAddress, RequestLength, useMultiWriteFunction, null);
                case nameof(DateTime):
                    return new ModbusDateTimePoint(GetSlaveAddress(memberInfo.ReflectedType), writable, Address, SkipFirstByte, Endian, RequestAddress, RequestLength, useMultiWriteFunction, null);
                case "Byte[]":
                    return new ModbusByteArrayPoint(GetSlaveAddress(memberInfo.ReflectedType), writable, Address,
                        BytesLength, SkipFirstByte, Endian, RequestAddress, RequestLength, useMultiWriteFunction, null);
                case nameof(String):
                    return new ModbusStringPoint(GetSlaveAddress(memberInfo.ReflectedType), writable, Address,
                        BytesLength, System.Text.Encoding.GetEncoding(Encoding), SkipFirstByte, Endian, RequestAddress, RequestLength, useMultiWriteFunction, null);
                case nameof(DateTimeOffset):
                    return new ModbusUnixTimePoint(GetSlaveAddress(memberInfo.ReflectedType), writable, Address, UnixTimeIsMilliseconds, UnixTimeScalePowerOf10,
                        BytesLength, SkipFirstByte, Endian, RequestAddress, RequestLength, useMultiWriteFunction, null);
                default:
                    return null;
            };
        }
    }
}
