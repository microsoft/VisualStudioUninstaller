Visual Studio Uninstaller
=========

Visual Studio Uninstallation sometimes can be unreliable and often leave out a lot of unwanted artifacts.  Visual Studio Uninstaller is designed to thoroughly and reliably remove these unwanted artifacts.

Status
========
Shipping

Contributing and building this project
========
See CONTRIBUTING.md

Goals/Vision/Scope
========
Our goal is to provide a way to thoroughly and reliably remove Visual Studio.  This program first attempts to force uninstall Visual Studio from top down, and then remove any remaining MSIs and MSUs.  This program will work on any BURN based Visual Studio; that means this program is only capable of removing Visual Studio 2012 and above.

Mailing list/contacts/forums
========
TBD

Usage
========

**How to debug Total Uninstaller remotely?**

Uncommenting the following lines will not execute the actual uninstall.  A debug flag will be added in the future.

           //uti.bDebug = op.Debug; ip.DebugReporting = op.Debug;
           //ip.DoNotExecuteProcess = true;

Note: Do not run this on your development machine without setting the `DoNotExecuteProcess` flag.  This will prevent the application from uninstalling the very development environment you are working from.   

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

  1. The user identifies which SKU of which release he wants to uninstall.
  2. The user downloads one or more data files (from `DataFiles` folder) to a local folder.
  3. The user executes Total Uninstaller and then `dir` to the directory containing the config files.
  4. The user executes `load` command to the config files.
  5. The user executes `list` to show which SKU and release is selected and installed.
  6. The user executes `select` to select which SKU, release, version he wants to uninstall.

The user executes `uninstall` to perform the total uninstall.

Roadmap
========
TBD

Open issues
========
TBD

Guidelines
=========

These are general guidelines for source code within this solution.

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


