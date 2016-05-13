# JavaBuild  
Simple build program for java projects.  
This is for **windows-only**.  
Other platform support will be added soon.
  
## Supported Features  
  
### Java file compilation  
JavaBuild will compile all <code>.java</code> files listed in "files" array.  
You should omit file type extension <code>.java</code>  
  
### Java compile options  
Currently, only three options are supported.  
<code>-classpath</code> : Specifies where to find user class files.  
<code>-encoding</code> : Specifies character encoding used by source files.  
<code>-Werror</code> : Terminate compilation if warnings occur.  
  
Compile options can be added to **"CompileOptions"** object as **key-value pairs**.  
  
**"ClassPath"** for <code>-classpath</code> option.(type : **string**)  
**"CharacterEncoding"** for <code>-encoding</code> option.(type : **string**)  
**"TerminateOnWarning"** for <code>-Werror</code> option.(type : **boolean**)  
  
For options you do not want to specify,   
just omit option for boolean type, leave as empty string for string type.  
  
Default Values :  
**boolean** - **<code>false</code>**  
**string** - **<code>""</code>**
  
## Usage  
Below example code is for build file.  
It should have name <code>build.json</code>  
```json
{
  "files" : [
    "AnalyticalSolverCalc",
    "AnalyticalSolverMain"
  ],
  "mains" : [
    "AnalyticalSolverMain"
  ],
  "CompileOptions" : {
    "ClassPath" : "",
    "CharacterEncoding" : "",
    "TerminateOnWarning" : false
  }
}
```
  
After making <code>build.json</code> file, just place execution file - <code>JavaBuild.exe</code>  
in folder that have "files" there.  
**Run it!**
  
## Build execution file  
  
### Requirements  
**.NET Framework 4.6 or higher** - [Get .NET Framework 4.6.1](https://www.microsoft.com/ko-kr/download/details.aspx?id=49981)  
(Only for users who will build from source) **F# 4.0 or higher.**  
(Only for users who will build from source) **FSC(F# compiler)**  

### Build Executable - Visual Studio (2015 or higher)  
If you have F# installed, just **Run the solution!**  
  
### Build Executable - Command line  
Go to source folder and type below.  
<code>fsc -o:JavaBuild.exe --target:exe --standalone --staticlink:Newtonsoft.Json -r:Newtonsoft.Json Program.fs</code>  
  
## Download execution file  
Download the <code>JavaBuild.exe</code> file in <code>Download</code> folder.  
  
## Notice  
  
### JSON parsing    
JSON parsing uses <b>Newtonsoft.Json</b> library.  
They can be found at [http://www.newtonsoft.com](http://www.newtonsoft.com)  
License about this library can be found in above link.  

## License  
This program is open source under MIT license.  
  
Copyright (c) 2016 Jiung Hahm
  
Permission is hereby granted, free of charge, to any person
obtaining a copy of this software and associated documentation
files (the "Software"), to deal in the Software without
restriction, including without limitation the rights to use,
copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the
Software is furnished to do so, subject to the following
conditions:
  
The above copyright notice and this permission notice shall be
included in all copies or substantial portions of the Software.
  
THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
OTHER DEALINGS IN THE SOFTWARE.
