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

Road map
========
TBD

Open issues
========
TBD

Guidlines
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

Please follow the following guidance for coding standards:
https://msdn.microsoft.com/en-us/library/Ff926074.aspx?f=255&MSPPError=-2147217396


