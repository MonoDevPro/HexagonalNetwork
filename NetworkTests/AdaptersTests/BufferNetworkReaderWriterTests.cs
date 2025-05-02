using NetworkHexagonal.Adapters.Outbound.Networking.Serializer;
using NetworkHexagonal.Core.Application.Ports;
using System.Text;

namespace NetworkHexagonal.Tests.AdaptersTests;

public class BufferNetworkReaderWriterTests
{
    [Fact]
    public void ReadWrite_PrimitiveTypes_Works()
    {
        // Arrange
        var writer = new BufferNetworkWriter();
        
        // Act - Write data
        writer.Write(42);
        writer.Write(long.MaxValue);
        writer.Write(short.MinValue);
        writer.Write(ushort.MaxValue);
        writer.Write(uint.MaxValue);
        writer.Write(ulong.MaxValue);
        writer.Write(3.14159f);
        writer.Write(2.71828);
        writer.Write('A');
        writer.Write(true);
        writer.Write("Hello Network!");
        
        // Reset position for reading
        writer.Position = 0;
        var reader = new BufferNetworkReader(writer);
        
        // Assert - Read and verify
        Assert.Equal(42, reader.ReadInt());
        Assert.Equal(long.MaxValue, reader.ReadLong());
        Assert.Equal(short.MinValue, reader.ReadShort());
        Assert.Equal(ushort.MaxValue, reader.ReadUShort());
        Assert.Equal(uint.MaxValue, reader.ReadUInt());
        Assert.Equal(ulong.MaxValue, reader.ReadULong());
        Assert.Equal(3.14159f, reader.ReadFloat());
        Assert.Equal(2.71828, reader.ReadDouble());
        Assert.Equal('A', reader.ReadChar());
        Assert.True(reader.ReadBool());
        Assert.Equal("Hello Network!", reader.ReadString());
    }
    
    [Fact]
    public void ReadWrite_Arrays_Works()
    {
        var writer = new BufferNetworkWriter();
        
        // Prepare test data arrays
        var intArray = new[] { 1, 2, 3, 4, 5 };
        var floatArray = new[] { 1.1f, 2.2f, 3.3f, 4.4f, 5.5f };
        var longArray = new[] { long.MinValue, long.MaxValue, 0, 123456789L };
        var doubleArray = new[] { 1.1, 2.2, 3.3, 4.4, 5.5 };
        var stringArray = new[] { "one", "two", "three", "four", "five" };
        
        // Act - Write arrays
        writer.WriteArray(intArray);
        writer.WriteArray(floatArray);
        writer.WriteArray(longArray);
        writer.WriteArray(doubleArray);
        writer.WriteArray(stringArray);
        
        // Reset position for reading
        writer.Position = 0;
        var reader = new BufferNetworkReader(writer);
        
        // Assert - Read arrays and verify
        var readIntArray = reader.ReadIntArray();
        var readFloatArray = reader.ReadFloatArray();
        var readLongArray = reader.ReadLongArray();
        var readDoubleArray = reader.ReadDoubleArray();
        var readStringArray = reader.ReadStringArray();
        
        // Verify each array
        Assert.Equal(intArray.Length, readIntArray.Length);
        for (int i = 0; i < intArray.Length; i++)
            Assert.Equal(intArray[i], readIntArray[i]);
            
        Assert.Equal(floatArray.Length, readFloatArray.Length);
        for (int i = 0; i < floatArray.Length; i++)
            Assert.Equal(floatArray[i], readFloatArray[i]);
            
        Assert.Equal(longArray.Length, readLongArray.Length);
        for (int i = 0; i < longArray.Length; i++)
            Assert.Equal(longArray[i], readLongArray[i]);
            
        Assert.Equal(doubleArray.Length, readDoubleArray.Length);
        for (int i = 0; i < doubleArray.Length; i++)
            Assert.Equal(doubleArray[i], readDoubleArray[i]);
            
        Assert.Equal(stringArray.Length, readStringArray.Length);
        for (int i = 0; i < stringArray.Length; i++)
            Assert.Equal(stringArray[i], readStringArray[i]);
    }
    
    [Fact]
    public void ReadWrite_EmptyArrays_Works()
    {
        // Arrange
        var writer = new BufferNetworkWriter();
        
        // Act - Write empty arrays
        writer.WriteArray(Array.Empty<int>());
        writer.WriteArray(Array.Empty<float>());
        writer.WriteArray(Array.Empty<long>());
        writer.WriteArray(Array.Empty<double>());
        writer.WriteArray(Array.Empty<string>());
        
        // Reset position for reading
        writer.Position = 0;
        var reader = new BufferNetworkReader(writer);
        
        // Assert - Read empty arrays and verify
        var readIntArray = reader.ReadIntArray();
        var readFloatArray = reader.ReadFloatArray();
        var readLongArray = reader.ReadLongArray();
        var readDoubleArray = reader.ReadDoubleArray();
        var readStringArray = reader.ReadStringArray();
        
        // Verify each array is empty
        Assert.Empty(readIntArray);
        Assert.Empty(readFloatArray);
        Assert.Empty(readLongArray);
        Assert.Empty(readDoubleArray);
        Assert.Empty(readStringArray);
    }
    
    [Fact]
    public void ReadWrite_ByteArrays_Works()
    {
        // Arrange
        var writer = new BufferNetworkWriter();
        
        // Prepare test byte array
        byte[] data = new byte[100];
        for (int i = 0; i < data.Length; i++)
        {
            data[i] = (byte)(i % 256);
        }
        
        // Act - Write byte array
        writer.Write(data);
        
        // Reset position for reading
        writer.Position = 0;
        var reader = new BufferNetworkReader(writer);
        
        // Read back
        byte[] readData = reader.ReadByteArray();
        
        // Assert - verify bytes match
        Assert.Equal(data.Length, readData.Length);
        for (int i = 0; i < data.Length; i++)
        {
            Assert.Equal(data[i], readData[i]);
        }
    }
    
    [Fact]
    public void ReadWrite_LargeData_Works()
    {
        // Arrange
        var writer = new BufferNetworkWriter();
        
        // Create large test data
        const int arraySize = 10000;
        int[] largeIntArray = new int[arraySize];
        string[] largeStringArray = new string[100];
        StringBuilder sb = new StringBuilder();
        
        // Fill arrays with test data
        for (int i = 0; i < arraySize; i++)
        {
            largeIntArray[i] = i;
        }
        
        for (int i = 0; i < 1000; i++)
        {
            sb.Append($"Large string content {i}, ");
        }
        var largeString = sb.ToString();
        
        for (int i = 0; i < largeStringArray.Length; i++)
        {
            largeStringArray[i] = $"{largeString} - Instance {i}";
        }
        
        // Act - Write large data
        writer.WriteArray(largeIntArray);
        writer.Write(largeString);
        writer.WriteArray(largeStringArray);
        
        // Reset position for reading
        writer.Position = 0;
        var reader = new BufferNetworkReader(writer);
        
        // Read large data back
        var readLargeIntArray = reader.ReadIntArray();
        var readLargeString = reader.ReadString();
        var readLargeStringArray = reader.ReadStringArray();
        
        // Assert
        Assert.Equal(arraySize, readLargeIntArray.Length);
        Assert.Equal(largeString, readLargeString);
        Assert.Equal(largeStringArray.Length, readLargeStringArray.Length);
        
        // Spot check first, middle and last elements
        Assert.Equal(largeIntArray[0], readLargeIntArray[0]);
        Assert.Equal(largeIntArray[arraySize / 2], readLargeIntArray[arraySize / 2]);
        Assert.Equal(largeIntArray[arraySize - 1], readLargeIntArray[arraySize - 1]);
        
        Assert.Equal(largeStringArray[0], readLargeStringArray[0]);
        Assert.Equal(largeStringArray[50], readLargeStringArray[50]);
        Assert.Equal(largeStringArray[99], readLargeStringArray[99]);
    }
}