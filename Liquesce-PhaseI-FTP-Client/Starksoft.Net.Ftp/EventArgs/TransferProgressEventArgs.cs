/*
Copyright (c) 2007-2009 Benton Stark, Starksoft LLC (http://www.starksoft.com) 

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/

using System;

namespace Starksoft.Net.Ftp
{
   /// <summary>
   /// Event arguments to facilitate the transfer progress event.
   /// </summary>
   public class TransferProgressEventArgs : EventArgs
   {

      private readonly uint _bytesTransferred;
      private readonly uint _bytesPerSecond;
      private readonly TimeSpan _elapsedTime;

      /// <summary>
      /// Constructor.
      /// </summary>
      /// <param name="bytesTransferred">The number of bytes transferred.</param>
      /// <param name="bytesPerSecond">The data transfer speed in bytes per second.</param>
      /// <param name="elapsedTime">The time that has elapsed since the data transfer started.</param>
      public TransferProgressEventArgs(uint bytesTransferred, uint bytesPerSecond, TimeSpan elapsedTime)
      {
         _bytesTransferred = bytesTransferred;
         _bytesPerSecond = bytesPerSecond;
         _elapsedTime = elapsedTime;
      }

      /// <summary>
      /// The number of bytes transferred.
      /// </summary>
      public uint BytesTransferred
      {
         get { return _bytesTransferred; }
      }

      /// <summary>
      /// Gets the data transfer speed in bytes per second.
      /// </summary>
      public uint BytesPerSecond
      {
         get { return _bytesPerSecond; }
      }

      /// <summary>
      /// Gets the data transfer speed in kilobytes per second.
      /// </summary>
      public uint KilobytesPerSecond
      {
         get { return _bytesPerSecond / 1024; }
      }

      /// <summary>
      /// Gets the time that has elapsed since the data transfer started.
      /// </summary>
      public TimeSpan ElapsedTime
      {
         get { return _elapsedTime; }
      }


   }
}
