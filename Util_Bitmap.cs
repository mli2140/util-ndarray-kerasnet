using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace ASP.NET_Application_Test.Utils
{
    public class Util_Bitmap
    {
        public static Bitmap CreatBitmapFromBase64(string base64)
        {
            byte[] byteBuffer = Convert.FromBase64String(base64);
            MemoryStream memoryStream = new MemoryStream(byteBuffer);

            memoryStream.Position = 0;

            Bitmap bmpReturn = (Bitmap)Bitmap.FromStream(memoryStream);

            memoryStream.Close();

            return bmpReturn;
        }

        /// <summary>
        /// Convert Bitmap to Byte Array
        /// </summary>
        /// <param name="BitmapImage">Bitmap to convert</param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Plattformkompatibilität überprüfen", Justification = "<Ausstehend>")]
        public static byte[] BitmapToByteArray(Bitmap BitmapImage)
        {
            try
            {
                if (BitmapImage == null)
                {
                    throw new ArgumentNullException("Parameter BitmapImage is null");
                }

                int Width = BitmapImage.Width;
                int Height = BitmapImage.Height;
                BitmapData bmpData = BitmapImage.LockBits(new Rectangle(0, 0, Width, Height), ImageLockMode.ReadOnly, BitmapImage.PixelFormat);
                byte[] ByteArray = new byte[bmpData.Stride * Height];
                IntPtr BitmapPixelPtr = bmpData.Scan0;

                Marshal.Copy(BitmapPixelPtr, ByteArray, 0, bmpData.Stride * Height);

                BitmapImage.UnlockBits(bmpData);

                return ByteArray;
            }
            catch (Exception ex)
            {
                throw new Exception("Error during conversion from Bitmap to Byte Array", ex);
            }
        }

        /// <summary>
        /// Convert a 1, 3 or 4-channel Bitmap to a corresponding amount of Byte Arrays
        /// </summary>
        /// <param name="BitmapImage">Bitmap to convert</param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Plattformkompatibilität überprüfen", Justification = "<Ausstehend>")]
        public static byte[][] BitmapToByteArrayChannels(Bitmap BitmapImage)
        {
            try
            {
                if (BitmapImage == null)
                {
                    throw new ArgumentNullException("Parameter BitmapImage is null");
                }

                int NumChannels;
                switch (BitmapImage.PixelFormat)
                {
                    case PixelFormat.Format8bppIndexed:
                        NumChannels = 1;
                        break;
                    case PixelFormat.Format32bppRgb:
                    case PixelFormat.Format24bppRgb:
                        NumChannels = 3;
                        break;
                    case PixelFormat.Format32bppArgb:
                    case PixelFormat.Format32bppPArgb:
                        NumChannels = 4;
                        break;
                    default:
                        throw new ArgumentException($"Pixel Format \"{BitmapImage.PixelFormat}\" of provided Image is not supported. Please change Pixel Format.");
                }
                byte[][] Arrays = new byte[NumChannels][];

                int Width = BitmapImage.Width;
                int Height = BitmapImage.Height;
                BitmapData bmpData = BitmapImage.LockBits(new Rectangle(0, 0, Width, Height), ImageLockMode.ReadOnly, BitmapImage.PixelFormat);

                int StrideBytes;
                int Stride = bmpData.Stride;
                IntPtr BitmapPixelPtr = bmpData.Scan0;
                byte[] BitmapPixels;

                BitmapPixels = new byte[Stride * Height];
                Marshal.Copy(BitmapPixelPtr, BitmapPixels, 0, Stride * Height);

                switch (NumChannels)
                {
                    case 1:
                        StrideBytes = bmpData.Stride - Width;
                        if (StrideBytes > 0)
                        {
                            Arrays[0] = new byte[Width * Height];
                            for (int i = 0; i < Height; i++)
                            {
                                Array.Copy(BitmapPixels, i * Stride, Arrays[0], i * Width, Width);
                            }
                        }
                        else
                        {
                            Arrays[0] = new byte[Width * Height];
                            Marshal.Copy(BitmapPixelPtr, Arrays[0], 0, Width * Height);
                        }
                        break;
                    case 3:
                        StrideBytes = Stride - Width * 3;
                        if (BitmapImage.PixelFormat == PixelFormat.Format24bppRgb)
                        {
                            if (StrideBytes > 0)
                            {
                                Arrays = Deinterleave(RemoveStride(BitmapPixels, Width * 3, Stride), 3, 1, 0);
                            }
                            else
                            {
                                Arrays = Deinterleave(BitmapPixels, 3, 1, 0);
                            }
                        }
                        else
                        {
                            Arrays = Deinterleave(BitmapPixels, 3, 1, 1);
                        }
                        break;
                    case 4:
                        Arrays = Deinterleave(BitmapPixels, 4, 1, 0);
                        break;
                    default:
                        throw new Exception($"Channel Number {NumChannels} was not expected.");
                }
                BitmapImage.UnlockBits(bmpData);

                return Arrays;
            }
            catch (Exception ex)
            {
                throw new Exception("Error during conversion from Bitmap to Byte Array channels", ex);
            }
        }


        /// <summary>
        /// Deinterleaves a homogenious interleaved Byte Array with N fields to N separate Byte Arrays. Faster than Deinterleave() for large Arrays(> about 10.000 bytes; tested with 16 tasks)
        /// </summary>
        /// <param name="InterleavedArray">Interleaved Array to process</param>
        /// <param name="NumFields">Number of fields that constitute an element</param>
        /// <param name="FieldSize">Size of one field in bytes</param>
        /// <param name="NumPadBytes">Number of bytes at the end of an element that are not transferred to the Field arrays</param>
        /// <returns></returns>
        public static byte[][] Deinterleave(byte[] InterleavedArray, int NumFields, int FieldSize, int NumPadBytes)
        {
            if (InterleavedArray == null)
            {
                throw new ArgumentNullException(nameof(InterleavedArray));
            }

            if (NumFields == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(NumFields), "There needs to be at least one Field");
            }

            if (FieldSize == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(FieldSize), "FieldSize can't be 0");
            }

            int ElementSize = NumFields * FieldSize + NumPadBytes;
            if (InterleavedArray.Length % ElementSize != 0)
            {
                throw new ArgumentException("Length of InterleavedArray has to be an integer multiple of NumFields * FieldSize + NumPadBytes");
            }

            byte[][] Arrays = new byte[NumFields][];
            for (int i = 0; i < NumFields; i++)
            {
                Arrays[i] = new byte[InterleavedArray.Length / ElementSize];
            }

            for (int ElementIndex = 0; ElementIndex < InterleavedArray.Length / ElementSize; ElementIndex++)
            {
                for (int FieldIndex = 0; FieldIndex < NumFields; FieldIndex++)
                {
                    for (int SubFieldIndex = 0; SubFieldIndex < FieldSize; SubFieldIndex++)
                    {
                        Arrays[FieldIndex][ElementIndex * FieldSize + SubFieldIndex] = InterleavedArray[ElementIndex * ElementSize + FieldIndex * FieldSize + SubFieldIndex];
                    }
                }
            }
            return Arrays;
        }


        /// <summary>
        /// Removes Stride-ValidBytes bytes after every block of ValidBytes
        /// </summary>
        /// <param name="StridedArray">Array to remove stride from</param>
        /// <param name="ValidBytes">Length of continuous byte block in bytes</param>
        /// <param name="Stride">Stride length in bytes</param>
        /// <returns></returns>
        public static byte[] RemoveStride(byte[] StridedArray, int ValidBytes, int Stride)
        {
            if (StridedArray == null)
            {
                throw new ArgumentNullException(nameof(StridedArray));
            }

            if (StridedArray.Length % Stride != 0)
            {
                throw new ArgumentException("Length of StridedArray has to be an integer multiple of Stride");
            }

            if (ValidBytes > Stride)
            {
                throw new ArgumentOutOfRangeException(nameof(ValidBytes), "ValidBytes cannot be larger than Stride");
            }

            if (ValidBytes == Stride)
            {
                return StridedArray;
            }

            int Iterations = StridedArray.Length / Stride;
            byte[] ContinuousArray = new byte[Iterations * ValidBytes];
            for (int i = 0; i < Iterations; i++)
            {
                Array.Copy(StridedArray, i * Stride, ContinuousArray, i * ValidBytes, ValidBytes);
            }
            return ContinuousArray;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ArraysToRearrange"></param>
        /// <param name="NewOrder"></param>
        /// <returns></returns>
        public static byte[][] RearrangeArrays(byte[][] ArraysToRearrange, int[] NewOrder)
        {
            byte[][] RearrangedArrays = new byte[ArraysToRearrange.Length][];
            for (int i = 0; i < ArraysToRearrange.Length; i++)
            {
                RearrangedArrays[NewOrder[i]] = ArraysToRearrange[i];
            }
            return RearrangedArrays;
        }

        /// <summary>
        /// Interleaves N separate Byte Arrays homogeniously to a Byte Array with N fields. Faster than Interleave() for large Arrays(> some 10.000 bytes; tested with 16 tasks)
        /// </summary>
        /// <param name="ArraysToInterleave">Arrays to interleave</param
        /// <param name="FieldSize">Size of one field in bytes</param>
        /// <param name="NumTasks">Number of tasks that should process the Array in parallel</param>
        /// <returns></returns>
        public static byte[] InterleaveParallel(byte[][] ArraysToInterleave, int FieldSize, int NumTasks)
        {
            if (ArraysToInterleave == null)
            {
                throw new ArgumentNullException("InterleavedArray");
            }

            if (ArraysToInterleave.Length == 0)
            {
                throw new ArgumentException("ArraysToInterleave can't be empty");
            }

            if (FieldSize == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(FieldSize), "FieldSize can't be 0");
            }

            int NumFields = ArraysToInterleave.Length;

            int ElementArrayLength = ArraysToInterleave[0].Length;
            for (int i = 0; i < NumFields; i++)
            {
                if (ArraysToInterleave[i].Length != ElementArrayLength)
                {
                    throw new ArgumentException("Can't interleave Arrays of inconsistent length");
                }
            }

            if (ElementArrayLength % FieldSize != 0)
            {
                throw new ArgumentException("Lengths of Arrays to interleave have to be an integer multiple of FieldSize");
            }

            byte[] UninterleavedArray = new byte[NumFields * ElementArrayLength];

            for (int i = 0; i < NumFields; i++)
            {
                Array.Copy(ArraysToInterleave[i], 0, UninterleavedArray, i * ElementArrayLength, ElementArrayLength);
            }

            byte[] InterleavedArray = new byte[NumFields * ElementArrayLength];
            int ElementSize = NumFields * FieldSize;
            Task[] InterleavingTasks = new Task[NumTasks];
            for (int i = 0; i < NumTasks; i++)
            {
                int StartIndex = (UninterleavedArray.Length * i) / NumTasks;
                int EndIndex = (UninterleavedArray.Length * (i + 1)) / NumTasks;
                InterleavingTasks[i] = Task.Run(() => InterleavingTask(UninterleavedArray, InterleavedArray, FieldSize, ElementSize, ElementArrayLength, StartIndex, EndIndex));
            }

            Task.WaitAll(InterleavingTasks);

            return InterleavedArray;
        }

        private static void InterleavingTask(byte[] uninterleavedArray, byte[] interleavedarray, int fieldSize, int elementSize, int elementArrayLength, int startIndex, int endIndex)
        {
            for (int i = startIndex; i < endIndex; i++)
            {
                int Index = (int)((i % elementArrayLength) / fieldSize) * elementSize + (int)(i / elementArrayLength) * fieldSize + i % fieldSize;  //Element in interleaved Array + Field in Element + Offset for FieldSize
                interleavedarray[Index] = uninterleavedArray[i];
            }
        }
    }
}
