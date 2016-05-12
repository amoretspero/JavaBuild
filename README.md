# JavaBuild  
  
## Usage  
To be added.  
  
## Build execution  
  
### Requirements  
.NET Framework 4.6 or higher  
F# 4.0 or higher.  
fsc(F# compiler)  

### Build Executable - Visual Studio (2015 or higher)  
Run the solution!  
  
### Build Executable - Command line  
Go to source folder and type below.  
<code>fsc -o:JavaBuild.exe --target:exe --staticlink:Newtonsoft.Json -r:Newtonsoft.Json -r:Microsoft.Management.Infrastructure -r:System.Management.Automation Program.fs</code>  
  
## Notice  
  
### JSON parsing    
JSON parsing uses <b>Newtonsoft.Json</b> library.  
They can be found at <link rel="http://www.newtonsoft.com/json" value="http://www.newtonsoft.com/json"/>  
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