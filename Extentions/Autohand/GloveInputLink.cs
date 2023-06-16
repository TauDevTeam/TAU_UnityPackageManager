using System;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using UnityEngine;

namespace TAU_OpenGlove
{
    public class GloveInputLink
    {
        private NamedPipeClientStream pipe;

        public enum Handness
        {
            Left,
            Right
        }


        public GloveInputLink(Handness handness)
        {
            string hand = handness == Handness.Right ? "right" : "left";

            pipe = new NamedPipeClientStream(".", $"vrapplication\\input\\glove\\v2\\{hand}", PipeDirection.Out);

            Debug.Log($"Connecting to {hand} hand pipe...");
            try
            {
                pipe.Connect(2000);
            }
            catch (Exception e)
            {
                //if an error is thrown log the message
                Debug.Log(e.Message);
            }
            if (pipe.IsConnected)
                Debug.Log($"Connected! CanWrite:{pipe.CanWrite}");
            else
                Debug.Log("Connection failed");
        }


        //set all input values to default
        public void Relax()
        {
            Write(new InputData(true));
        }


        //send values to the driver
        public void Write(InputData input)
        {
            if (!pipe.IsConnected) return;

            int size = Marshal.SizeOf(input);
            Debug.Log(size);
            byte[] arr = new byte[size];

            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(input, ptr, true);
            Marshal.Copy(ptr, arr, 0, size);
            Marshal.FreeHGlobal(ptr);

            pipe.Write(arr, 0, size);
        }
    }
}
