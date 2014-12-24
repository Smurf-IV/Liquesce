#region Copyright (C)
// ---------------------------------------------------------------------------------------------------------------
//  <copyright file="ExpectedDetailResult.cs" company="Smurf-IV">
// 
//  Copyright (C) 2010-2013 Simon Coghlan (Aka Smurf-IV)
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
using System;

namespace Liquesce
{
   class ExpectedDetailResult : IComparable
   {
      public string DisplayName { get; set; }
      public string ActualFileLocation { get; set; }
      public int CompareTo(object obj)
      {
         ExpectedDetailResult other = obj as ExpectedDetailResult;
         return (other!= null)?DisplayName.CompareTo(other.DisplayName):0;
      }
   }
}
