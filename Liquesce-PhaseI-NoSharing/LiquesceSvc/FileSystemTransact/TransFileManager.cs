#region Copyright (C)
// ---------------------------------------------------------------------------------------------------------------
//  <copyright file="TransFileManager.cs" company="Smurf-IV">
// 
//  Copyright (C) 2012 Smurf-IV
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 2 of the License, or
//   any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program. If not, see http://www.gnu.org/licenses/.
//  </copyright>
//  <summary>
//  Url: http://Liquesce.codeplex.com/
//  Email: http://www.codeplex.com/site/users/view/smurfiv
//  </summary>
// --------------------------------------------------------------------------------------------------------------------
#endregion

using System.IO;
using ChinhDo.Transactions;

namespace LiquesceSvc.FileSystemTransact
{
   /// <summary>
   /// It is assumed that the permissions have already been set in order to use this in the correct scope.
   /// </summary>
   internal class TransFileManager : TxFileManager
   {
      public static void DeleteDirectory(string path)
      {
         if (IsInTransaction())
         {
            EnlistOperation(new DeleteDirectoryOperation(path));
         }
         else
         {
            Directory.Delete(path);
         }
      }
   }
}
