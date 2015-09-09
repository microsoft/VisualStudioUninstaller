# Contribute to the Microsoft Visual Studio Uninstaller

In order to contribute, you will need to be able to build the source, deploy to test and run automated tests.
Please fork and send pull requests.

## Building From Source

### Clone the repo
```bash
git clone https://github.com/Microsoft/vs.uninstaller
```

### Build Pre-reqs

You will need Visual Studio 2013 Community or greater in order to build this project.
```bash
Download it for free here: https://www.visualstudio.com/en-us/products/visual-studio-community-vs.aspx 
```

### Build
Open Uninstaller.sln in Visual Studio
```bash
Build Solution
```

This builds the project and output binaries to ./bin/Debug

### Run Tests

Please run all unit tests in the solution prior to PR.

```bash
Unit tests
```
