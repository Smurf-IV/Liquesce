using System;
using System.Collections.Generic;

namespace LiquesceSvcMEF
{
   public interface IManagement
   {
      /// <summary>
      /// Used to force this Extension to read it's config information
      /// </summary>
      /// <param name="mountPoint">Ofset into the Config information as there may be more than one mount</param>
      /// <param name="holdOffBufferBytes">When creating new objects, this is used to move to the next calculated location</param>
      void Initialise(string mountPoint, UInt64 holdOffBufferBytes);

      /// <summary>
      /// Called to get this thing going
      /// </summary>
      void Start();

      /// <summary>
      /// Called to exit the functions / threads, so be nice because this is normally 
      /// the service closing and will have limited time before the rug is pulled
      /// </summary>
      void Stop();

      /// <summary>
      /// Details to be passed in of the base mountPoint information
      /// </summary>
      List<string> SourceLocations { set; }

      /// <summary>
      /// Details to be passed in as the shares are "discovered" for the mountPoint
      /// </summary>
      List<string> KnownSharePaths { set; }

   }
}
