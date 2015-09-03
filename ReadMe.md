Guidlines
=========

These are general guidelines for source code within this solution.

Native code
-----------

* Parameters should be declared with SAL annotation.
* Input string parameters should be declared as LPCWSTR.
* Output string parameters should be declared as CStringW& references.
* Class members should not be references or pointers, but be created and destroyed with the owning class.
