using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using DokanNet;
using NLog;
using NUnit.Framework;

namespace DokanTesting
{
   [TestFixture]
   [Description("These tests excercise the mount### API's.")]
   [Category("Mount")]
   public class MountTests
   {
      static private readonly Logger Log = LogManager.GetCurrentClassLogger();
      private List<string> mountedPoints = new List<string>();

      [TestFixtureSetUp]
      public void Init()
      {
         Log.Warn("Test @ [{0}]", "Warn");
         Log.Debug("Test @ [{0}]", "Debug");
         Log.Trace("Test @ [{0}]", "Trace");
         Log.Info("DokanVersion:[{0}], DokanDriverVersion[{1}]", Dokan.DokanVersion(), Dokan.DokanDriverVersion());
         Dokan.DokanSetDebugMode(true);

      }

      [TestFixtureTearDown]
      public void Dispose()
      {
         foreach (string point in mountedPoints)
         {
            if (point.Length == 1)
               Dokan.DokanUnmount(point[0]);
            else
               Dokan.DokanRemoveMountPoint(point);
         }
      }


      [SetUp]
      public void SetUp()
      {
      }

      [TearDown]
      public void TearDown()
      {
      }



      [Test]
      [Description("Check that it is possible to perform a simple mount.")]
      public void A010SimpleLetterMount()
      {
         mountedPoints.Add("M");
         DokanOptions options = new DokanOptions
            {
               MountPoint = "M",
               ThreadCount = 1,
               DebugMode = true
            };
         TestLayer testLayer = new TestLayer();
         ThreadPool.QueueUserWorkItem(testLayer.Start, options);


         Assert.That(() => CommonFuncs.CheckExistenceOfMount(options.MountPoint), Is.True.After(50000, 100), "CheckExistenceOfMount");

         //Assert.That(() => testLayer.RetVal, Is.EqualTo(Dokan.DOKAN_SUCCESS).After(500, 50),
         //            "Expected result after 500ms");


      }

      [Test]
      [Description("Check that it is possible to perform a simple mount.")]
      public void A020SimpleDirMount()
      {
         mountedPoints.Add("C:\\blam\\test");
         DokanOptions options = new DokanOptions
         {
            MountPoint = "C:\\blam\\test",
            ThreadCount = 1,
            DebugMode = true
         };
         TestLayer testLayer = new TestLayer();
         ThreadPool.QueueUserWorkItem(testLayer.Start, options);
         Thread.Sleep(20*1000);

         //Assert.That(() => CommonFuncs.CheckExistenceOfMount(options.MountPoint), Is.True.After(50000, 100), "CheckExistenceOfMount");

      }

   }

   public class TestLayer
   {
      public TestLayer()
      {
         RetVal = Dokan.DOKAN_MOUNT_ERROR;
      }

      public void Start(object state)
      {
         DokanOptions options = state as DokanOptions;
         IDokanOperations proxy = new TestDokanOperations();
         RetVal = Dokan.DokanMain(options, proxy);
      }

      public int RetVal { get; set; }
   }
}
