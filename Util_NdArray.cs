using Numpy;
using Numpy.Models;
using Python.Runtime;
using System.Runtime.InteropServices;

namespace ASP.NET_Application_Test.Utils
{
    public class Util_NdArray
    {
        /// <summary>
        /// Creates NDarray in unmanaged python Heap from managed byte[] in .NET Heap.
        /// </summary>
        /// <param name="bytes">Byte Array containing integer Representations of Bytes</param>
        /// <param name="shape">Shape of Image that is represented through the byte[]</param>
        /// <returns>NDarray with Shape of Image</returns>
        public static NDarray CreateNDarrayFromByteArray(byte[] bytes, int[] shape)
        {
            using (Py.GIL())
            {
                int index = 0;
                // Convert byte representations of byte array to float values
                float[] floatReprByteArray = new float[bytes.Length];

                // Iterating over byte array and casting byte representations to float values
                foreach (byte b in bytes)
                {
                    floatReprByteArray[index] = (float)b;
                    index++;
                }
                // Create byte array representing the float values as bytes
                var byteArray = new byte[floatReprByteArray.Length * sizeof(float)];
                Buffer.BlockCopy(floatReprByteArray, 0, byteArray, 0, byteArray.Length);
                // Create a flat NDarray that fits the size of image shape

                var doubles = np.zeros(new Shape(shape[0] * shape[1] * shape[2]), np.float32);
                var ctypes = doubles.PyObject.ctypes;
                long ptr = ctypes.data;

                // Copying byte array values from managed .NET heap to managed python heap
                // NDarray now contains float values and not the byte representations of float values
                Marshal.Copy(byteArray, 0, new IntPtr(ptr), byteArray.Length);
                // Resizing Ndarray to fit the shape of Image
                doubles.resize(new Shape(shape[0], shape[1], shape[2]));
                return doubles;
            }
        }
    }
}
