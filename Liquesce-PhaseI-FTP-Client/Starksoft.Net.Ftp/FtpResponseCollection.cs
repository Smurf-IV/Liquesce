/*
 *  Authors:  Benton Stark
 * 
 *  Copyright (c) 2007-2009 Starksoft, LLC (http://www.starksoft.com) 
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 * 
 */

using System;
using System.Collections.Generic;
using System.Text;


namespace Starksoft.Net.Ftp
{
   /// <summary>
   /// Ftp response collection.
   /// </summary>
   public class FtpResponseCollection : List<FtpResponse>
   {
      /// <summary>
      /// To bused to reverse the line that have come over the Data connection
      /// </summary>
      /// <param name="transferText"></param>
      public FtpResponseCollection(string transferText)
      {
         char[] crlfSplit = new char[2];
         crlfSplit[0] = '\r';
         crlfSplit[1] = '\n';
         foreach (string line in transferText.Split(crlfSplit, StringSplitOptions.RemoveEmptyEntries))
         {
            Add(new FtpResponse(line));
         } 
      }

      ///<summary>
      ///</summary>
      public FtpResponseCollection()
      {
      }

      /// <summary>
      /// Get the raw FTP server supplied reponse text.
      /// </summary>
      /// <returns>A string containing the FTP server response.</returns>
      public string GetRawText()
      {
         StringBuilder builder = new StringBuilder();
         ForEach(item =>
         {
            builder.Append(item.RawText);
            builder.Append("\r\n");
         });
         return builder.ToString();
      }

      /// <summary>
      /// Get the last server response from the FtpResponseCollection list.
      /// </summary>
      /// <returns>FtpResponse object.</returns>
      public FtpResponse GetLast()
      {
         return Count == 0 ? new FtpResponse() : this[Count - 1];
      }
   }
}