Visual Studio Uninstaller
=========

This executable is designed to clean up and delete Preview, RC and final releases of Visual Studio 2013 and Visual Studio 2015. It is designed to be used as a final resort to clean up a system of remaining artifacts from a non-successful installation, instead of having to reimage the machine.

WARNING: running this application may stop earlier remaining installations of Visual Studio 2012 and earlier from working, because Visual Studio 2012 and below share MSI upgrade code with Visual Studio 2013 and above.

Download: https://github.com/Microsoft/VisualStudioUninstaller/releases

How it works?
========

This app finds and uninstall every Preview/RC/RTM release of Visual Studio 2013 and 2015.  It will first execute uninstall command on the bundle, and then it will uninstall any stale MSIs. The application contains a master list of Bundle IDs and upgrade codes for every MSI ever chained in by Visual Studio 2013-2015.  It will not uninstall MSU or MSIs that marked as ReallyPermanent.  

Status
========
Shipped

Contributing and building this project
========
See CONTRIBUTING.md

Goals/Vision/Scope
========
Our goal is to provide a way to thoroughly and reliably remove Visual Studio.  This program first attempts to force uninstall Visual Studio from top down, and then remove any remaining MSIs and MSUs.  This program will work on any BURN based Visual Studio; that means this program is only capable of removing Visual Studio 2012 - 2015.

Mailing list/contacts/forums
========
https://www.visualstudio.com/support/support-overview-vs 

Usage
========

**How to debug Total Uninstaller remotely?**

IMPORTANT: Do not run this on your development machine without setting the `DoNotExecuteProcess` flag.  This will prevent the application from uninstalling the very development environment you are working from.   

To get the most out of the debug experience, I recommend the following:

  1. Create a VM with Dev14 installed.
  2. Start the 64-bit remote debugger with administrative privileges.
  3. Copy the debug Bin directory to the VM.
  4. Run the application with Administrative privileges.
  5. Create a snapshot of the machine using Hyper-V.
  6. Start a remote debugging session to your VM and attach.
  7. Step through to your hearts delight.
  8. If you find something you don’t like, restore the snapshot and recopy the Bin directory and go to step 6 again.

**Using Total Uninstall:**

  1. Download and unzip the zip file to a folder.
  2. Open cmd.exe with Administrative privileges
  2. Execute Setup.ForcedUninstall.exe
  3. Press Y and hit enter to run the application.
  4. If the application ask to reboot the system, please reboot the system, and rerun this application again.

**Commands:**

  1. help or /help or /? : Print usage.
  2. break : run the application and pause until the user hit any key.
  3. noprocess : run the application but does not uninstall anything.

Roadmap
========
1. Periodically update of the Total Uninstaller to ensure the data used for uninstallation is up to date with the most recent Visual Studio releases.

Open issues
========
Please file an issue request as necessary.

Guidelines
=========
These are general guidelines for source code within this solution:

Native code
-----------

* Parameters should be declared with SAL annotation.
* Input string parameters should be declared as LPCWSTR.
* Output string parameters should be declared as CStringW& references.
* Class members should not be references or pointers, but be created and destroyed with the owning class.


Managed code
-----------

Please follow these coding standards:
https://msdn.microsoft.com/en-us/library/Ff926074.aspx?f=255&MSPPError=-2147217396
