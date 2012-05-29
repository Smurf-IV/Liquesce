#region Copyright (C)
// ---------------------------------------------------------------------------------------------------------------
//  <copyright file="DeleteDirectoryOperation.cs" company="Smurf-IV">
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
   class DeleteDirectoryOperation : IRollbackableOperation
   {
      private readonly string path;
      private bool existed;

      /// <summary>
      /// Instantiates the class.
      /// </summary>
      /// <param name="path">The directory path to create.</param>
      public DeleteDirectoryOperation(string path)
      {
         this.path = path;
      }

      public void Rollback()
      {
         if (existed)
         {
            Directory.CreateDirectory(path);
         }
      }

      public void Execute()
      {
         existed = Directory.Exists(path);
         if (existed)
            Directory.Delete(path, false);
      }
   }
}
