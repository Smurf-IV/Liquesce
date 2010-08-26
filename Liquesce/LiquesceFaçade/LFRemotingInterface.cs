using System;
using System.Collections;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using NLog;

namespace LiquesceFaçade
{
   public class LFRemotingInterface
   {
      static private readonly Logger Log = LogManager.GetCurrentClassLogger();

      /// <summary>
      /// Target machine Uri
      /// </summary>
      protected Uri targetUri;

      public string HostTarget
      {
         get { return targetUri.Host; }
      }

      /// <summary>
      /// Remoting interface thing
      /// </summary>
      public ILiquesce theFacade;

      /// <summary>
      /// Something to ensure that the Remoting is initialized once
      /// </summary>
      static protected bool remotingInterfaceInitialised = false;

      /// <summary>
      /// Constructor
      /// </summary>
      /// <param name="targetUri">Target Uri</param>
      public LFRemotingInterface(Uri targetUri)
      {
         this.targetUri = targetUri;
      }

      /// <summary>
      /// Do what is needed to make the stuff work
      /// </summary>
      /// <returns>This will throw an exception if it fails</returns>
      public void Init()
      {
         if (!remotingInterfaceInitialised)
         {
            lock (this)
            {
               if (!remotingInterfaceInitialised)
               {
                  try
                  {
                     Log.Debug("InitRemoting");
                     InitRemoting();
                  }
                  catch (Exception ex)
                  {
                     Log.ErrorException("Init: ", ex);
                     //throw;
                  }
                  // We did not throw, So we must be registered
                  remotingInterfaceInitialised = true;
               }
            }
         }
         System.Security.Permissions.SecurityPermission sp = new System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityPermissionFlag.RemotingConfiguration);
         Log.Debug("Demanding SecurityPermissionFlag.RemotingConfiguration");
         sp.Demand();
         Log.Info(sp.ToXml());

         Log.Info("Create a newFacade");
         theFacade = (ILiquesce)Activator.GetObject(typeof(ILiquesce), targetUri.AbsoluteUri);
      }

      /// <summary>
      /// Initialise the .NET remoting. Registers the required remotable object if required and 
      /// registers with the remote objects.
      /// NB. This done programatically as an exception is thrown when the remoting is
      /// configured via the standard configuration file approach.
      /// </summary>
      private void InitRemoting()
      {
         try
         {
            // Attempt to get the errors as they are created rather than just single sided.
            RemotingConfiguration.CustomErrorsMode = CustomErrorsModes.Off;
            // Creating a custom formatter for a TcpChannel sink chain.
            BinaryServerFormatterSinkProvider provider = new BinaryServerFormatterSinkProvider
                                                            {
                                                               TypeFilterLevel =
                                                                  System.Runtime.Serialization.Formatters.
                                                                  TypeFilterLevel.Full
                                                            };
            // Creating the IDictionary to set the port on the channel instance.
            IDictionary props = new Hashtable {{"port", "7012"}, {"name", "tcp2"}, {"secure", "false"}};
            // Pass the properties for the port setting and the server provider in the server chain argument. (Client remains null here.)
            TcpChannel chan = new TcpChannel(props, null, provider);
            ChannelServices.RegisterChannel(chan, false);
            Log.Info("TCP server channel registered");
         }
         catch (RemotingException ex)
         {
            Log.ErrorException("InitRemoting Barfed[1]: ", ex);
            Log.Warn("InitRemoting states it already has a registered channel");
         }
         catch (Exception ex)
         {
            Log.ErrorException("InitRemoting Barfed[2]: ", ex);
            throw;
         }
      }

   }
}