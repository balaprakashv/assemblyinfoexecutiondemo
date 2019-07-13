using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace AsmblyInfo
{
    /// <summary>
    /// Application: AsmblyInfo.exe
    /// Written by: Eric Niemiec
    /// Date: 5/30/2008
    /// Contact: eric.niemiec@servicebrands.com
    /// </summary>
    class Program
    {
        /// <summary>
        /// This application is designed to modify the version information, company,
        /// copyright and product settings in AssemblyInfo classes in entire solutions or
        /// individual project files.  It was intended to be called prior to building the 
        /// application(s) using something like MSBuild from a build script that builds many
        /// projects or solutions that all require the version information to match or be 
        /// set prior to the build.  
        /// For example: (Where %Version% is equal to "2.1.37")
        /// AsmblyInfo C:\Data\Code\AssemblyInfoTest\AssemblyInfoTest.sln /version:%Version%
        /// MSBuild C:\Data\Code\AssemblyInfoTest\AssemblyInfoTest.sln /target:Build /property:Configuration=%Config% /Property:Platform=x86
        /// 
        /// *This application has been tested on VB and C# projects and solution files but
        /// *has NOT been tested on any web applications!  It will handle solutions with multiple
        /// *project files and solutions with both VB and C# project files in them.
        ///
        /// Eclipsys Corporation revisions include:
        /// a) support for C++ files, including VS 2010 C++ projects (.vcxproj)
        /// b) revise assemblyversion to "major.minor.0.0"
        /// c) only update values if they differ from what is already there
        /// d) if an entry value is "" replace it with the non-value parameter value
        /// e) only update the file if any of the existing values have actually changed
        /// f) adding missing lines to the end of the assemblyinfo file (i.e., fileversion, company, 
        ///    copyright, product, assemblyversion), if values have been provided by parameters
        /// g) only add missing lines if parameter "/CreateMissingLines" is specified on the command line
        /// h) only reset the file attributes if actually updating or writing to the assemblyinfo file
        /// i) make a number of comparisons case insensitive
        /// j) use a full four part number for AssemblyVerion if parameter "/forceAssemblyVersion" is 
        ///    specified on the command line
        /// k) support for .SQLproj files
        /// 
        /// 
        /// Examples of use:
        /// C:\Data\Code\Temp\AssemblyInfoTest\AssemblyInfoTest.csproj /version:2.01.12.0 /copyright:"My Copyright!" /product:"My .Net Suite" /company:"My Company"
        /// C:\Data\Code\Temp\AssemblyInfoTest\AssemblyInfoTest.sln /major:6 /minor:7 /build:8 /revision:9876  /copyright:"My Copyright" /product:"My .Net Suite" /company:"My Company"
        /// C:\Data\Code\Temp\AssemblyInfoTestVB1\AssemblyInfoTestVB1\AssemblyInfoTestVB1.vbproj /major:6 /minor:6 /build:6 /revision:6666  /copyright:"My Copyright" /product:"My .Net Suite" /company:"My Company"
        /// C:\Data\Code\Temp\AssemblyInfoTestVB1\AssemblyInfoTestVB1\AssemblyInfoTestVB1.sln /major:7 /minor:7 /build:7 /revision:7777 /copyright:"My Copyright" /product:"My .Net Suite" /company:"My Company"
        /// </summary>
        /// <param name="args">
        /// First parameter must be the path to a project file or solution file.
        /// Other optional parameters are:
        /// /version OR /major, /minor, /build and /revision
        /// /copyright
        /// /company
        /// /product
        /// /createmissinglines
        /// /forceassemblyversion
        /// </param>
        /// <returns>0 for success, 1 for failure</returns>
        static int Main(string[] args)
        {
            // any arguements passed?
            WriteConsoleInfo("Starting AsmblyInfo...", false);
            if (args.Length == 0)
            {
                // no arguements passed
                return WriteConsoleInfo("No arguements were passed.", true);
            }

            // get the base file and make sure it exists
            string baseFile = args[0];
            if (!File.Exists(baseFile))
            {
                // base file does not exist
                return WriteConsoleInfo("Specified file does not exist: '" + baseFile + "'", true);
            }

            // determine if version paramter was passed?
            Version fullNewVersion = null;
            string fullNewVersionString = string.Empty;
            string versionMajor = string.Empty;
            string versionMinor = string.Empty;
            string versionBuild = string.Empty;
            string versionRevison = string.Empty;
            string fullVersionParm = GetCmdLineParmValue("/version:", args);
            if (fullVersionParm.Length > 0)
            {
                try
                {
                    // tack on the revision if not passed
                    // (this will be a common issue)
                    string[] versionParts = fullVersionParm.Split(new char[] { '.' });
                    if (versionParts.Length == 3)
                    {
                        fullVersionParm += ".0";
                    }

                    // try to parse the full version passed
                    fullNewVersion = new Version(fullVersionParm);
                }
                catch (Exception ex)
                {
                    // unable to parse the version passed in to a version object
                    return WriteConsoleInfo("Invalid full version passed: '" + fullVersionParm + "'", true);
                }

                // make sure all the parts were passed
                if (fullNewVersion.Major == -1)
                {
                    return WriteConsoleInfo("When passing the /version parameter, you must pass the major version.", true);
                }
                if (fullNewVersion.Minor == -1)
                {
                    return WriteConsoleInfo("When passing the /version parameter, you must pass the minor version.", true);
                }
                if (fullNewVersion.Build == -1)
                {
                    return WriteConsoleInfo("When passing the /version parameter, you must pass the build number.", true);
                }
                if (fullNewVersion.Revision == -1)
                {
                    return WriteConsoleInfo("When passing the /version parameter, you must pass the revision number.", true);
                }

                // if full version is passed, make sure other version parts are not passed
                if ((GetCmdLineParmValue("/major:", args).Length > 0) ||
                    (GetCmdLineParmValue("/minor:", args).Length > 0) ||
                    (GetCmdLineParmValue("/build:", args).Length > 0) ||
                    (GetCmdLineParmValue("/revision:", args).Length > 0))
                {
                    // when the full version is passed, version parts may not also be passed
                    return WriteConsoleInfo("When /version is passed, /major, /minor, /build " +
                        "and /revision are not allowed", true);

                }
            }
            else
            {
                int testIntResult;
                // handle major version
                versionMajor = GetCmdLineParmValue("/major:", args);
                if (!int.TryParse(versionMajor, out testIntResult))
                {
                    // major version number is not an int
                    return WriteConsoleInfo("Major version is not an int: '" + versionMajor + "'", true);
                }
                // handle minor version
                versionMinor = GetCmdLineParmValue("/minor:", args);
                if (!int.TryParse(versionMinor, out testIntResult))
                {
                    // minor version number is not an int
                    return WriteConsoleInfo("Minor version is not an int: '" + versionMinor + "'", true);
                }
                // handle build version
                versionBuild = GetCmdLineParmValue("/build:", args);
                if (!int.TryParse(versionBuild, out testIntResult))
                {
                    // build number is not an int
                    return WriteConsoleInfo("Build number is not an int: '" + versionBuild + "'", true);
                }
                // handle revision number
                versionRevison = GetCmdLineParmValue("/revision:", args);
                if (!int.TryParse(versionRevison, out testIntResult))
                {
                    // revision number is not an int
                    return WriteConsoleInfo("Revision number is not an int: '" + versionRevison + "'", true);
                }
            }

            // determine if company paramter was passed?
            string newCompany = GetCmdLineParmValue("/company:", args);

            // determine if copyright paramter was passed?
            string newCopyright = GetCmdLineParmValue("/copyright:", args);

            // determine if product paramter was passed?
            string newProduct = GetCmdLineParmValue("/product:", args);

            // determine if missing lines are to be created (assuming values have been specified)?
            Boolean createMissingLines = CmdLineParmExists("/createmissinglines", args);

            // determine if the AssemblyVersion processing is supposed to use the full four part version info (assuming values have been specified)?
            Boolean forceFullAssemblyVersion = CmdLineParmExists("/forceassemblyversion", args);

            // determine if base file is a project file or a solution file
            List<string> projFiles = new List<string>();
            if ((baseFile.ToLower().EndsWith(".csproj")) || (baseFile.ToLower().EndsWith(".vbproj")) || (baseFile.ToLower().EndsWith(".vcproj")) || (baseFile.ToLower().EndsWith(".vcxproj")) || (baseFile.ToLower().EndsWith(".sqlproj")))
            {
                // add the project file to the list of project files
                projFiles.Add(baseFile);
            }
            else if (baseFile.ToLower().EndsWith(".sln"))
            {
                // extract the project files from the solution file first
                WriteConsoleInfo("Processing '" + baseFile.Substring(baseFile.LastIndexOf(@"\") + 1) + "'...", false);
                string[] solutionFileLines = File.ReadAllLines(baseFile);
                FileInfo fileInfo = new FileInfo(baseFile);
                if (solutionFileLines.Length > 0)
                {
                    // loop through all of the lines in the solution file
                    for (int i = 0; i < solutionFileLines.Length; i++)
                    {
                        // get the line to work with
                        string line = solutionFileLines[i];
                        if (line.Length > 2 && line.ToLower().Contains("scc")==false)
                        {
                            // does this line contain a project file?
                            if (line.ToLower().Contains(".csproj"))
                            {
                                // get the relative path to the project file and add it
                                // on to the base address of the solution file
                                string projFilePath = line.Substring(0, line.IndexOf(".csproj\"") + 7);
                                projFilePath = projFilePath.Substring(projFilePath.LastIndexOf(Convert.ToChar(34)) + 1);
                                projFiles.Add(fileInfo.DirectoryName + "\\" + projFilePath);
                            }
                            else if (line.ToLower().Contains(".vbproj"))
                            {
                                // get the relative path to the project file and add it
                                // on to the base address of the solution file
                                string projFilePath = line.Substring(0, line.IndexOf(".vbproj\"") + 7);
                                projFilePath = projFilePath.Substring(projFilePath.LastIndexOf(Convert.ToChar(34)) + 1);
                                projFiles.Add(fileInfo.DirectoryName + "\\" + projFilePath);
                            }
                            else if (line.ToLower().Contains(".vcproj"))
                            {
                                // get the relative path to the project file and add it
                                // on to the base address of the solution file
                                string projFilePath = line.Substring(0, line.IndexOf(".vcproj\"") + 7);
                                projFilePath = projFilePath.Substring(projFilePath.LastIndexOf(Convert.ToChar(34)) + 1);
                                projFiles.Add(fileInfo.DirectoryName + "\\" + projFilePath);
                            }
                            else if (line.ToLower().Contains(".vcxproj"))
                            {
                                // get the relative path to the project file and add it
                                // on to the base address of the solution file
                                string projFilePath = line.Substring(0, line.IndexOf(".vcxproj\"") + 8);
                                projFilePath = projFilePath.Substring(projFilePath.LastIndexOf(Convert.ToChar(34)) + 1);
                                projFiles.Add(fileInfo.DirectoryName + "\\" + projFilePath);
                            }
                            else if (line.ToLower().Contains(".sqlproj"))
                            {
                                // get the relative path to the project file and add it
                                // on to the base address of the solution file
                                string projFilePath = line.Substring(0, line.IndexOf(".sqlproj\"") + 8);
                                projFilePath = projFilePath.Substring(projFilePath.LastIndexOf(Convert.ToChar(34)) + 1);
                                projFiles.Add(fileInfo.DirectoryName + "\\" + projFilePath);
                            }
                        }
                    }
                }
            }
            else
            {
                // unknown file type passed
                return WriteConsoleInfo("Unknown file type passed: '" + baseFile + "'", true);
            }

            // extract the assembly info files from the project files
            List<string> assInfoFiles = new List<string>();
            foreach (string projFile in projFiles)
            {
                //Console.WriteLine(projFile);
                // does this file exist?
                if (!File.Exists(projFile))
                {
                    // project file does not exist
                    return WriteConsoleInfo("Specified file does not exist: '" + projFile + "'", true);
                    
                }
                // loop through all of the project files in the list
                WriteConsoleInfo("Processing '" + projFile.Substring(projFile.LastIndexOf(@"\") + 1) + "'...", false);
                string[] projFileLines = File.ReadAllLines(projFile);
                FileInfo fileInfo = new FileInfo(projFile);
                if (projFileLines.Length > 0)
                {
                    // loop through all of the lines in the project file
                    for (int i = 0; i < projFileLines.Length; i++)
                    {
                        // get the line to work with
                        string line = projFileLines[i];
                        if (line.Length > 2)
                        {
                            // does this line contain an assemblyInfo file?
                            if (line.ToLower().Contains("assemblyinfo.cs") && line.ToLower().Contains("include="))
                            {
                                // get the relative path to the assembly file and add it
                                // on to the base address of the project file
                                string assInfoFilePath = line.Substring(line.IndexOf(Convert.ToChar(34)) + 1);
                                assInfoFilePath = assInfoFilePath.Substring(0, assInfoFilePath.IndexOf(Convert.ToChar(34)));
                                assInfoFiles.Add(fileInfo.DirectoryName + "\\" + assInfoFilePath);
                            }
                            else if (line.ToLower().Contains("assemblyinfo.vb") && line.ToLower().Contains("include="))
                            {
                                // get the relative path to the assembly file and add it
                                // on to the base address of the project file
                                string assInfoFilePath = line.Substring(line.IndexOf(Convert.ToChar(34)) + 1);
                                assInfoFilePath = assInfoFilePath.Substring(0, assInfoFilePath.IndexOf(Convert.ToChar(34)));
                                assInfoFiles.Add(fileInfo.DirectoryName + "\\" + assInfoFilePath);
                            }
                            else if (line.ToLower().Contains("assemblyinfo.cpp") && (line.ToLower().Contains("relativepath=") || line.ToLower().Contains("include=")))
                            {
                                // get the relative path to the assembly file and add it
                                // on to the base address of the project file
                                string assInfoFilePath = line.Substring(line.IndexOf(Convert.ToChar(34)) + 1);
                                assInfoFilePath = assInfoFilePath.Substring(0, assInfoFilePath.IndexOf(Convert.ToChar(34)));
                                assInfoFiles.Add(fileInfo.DirectoryName + "\\" + assInfoFilePath);
                            }
                        }
                    }
                }
            }

            // loop through all of the assembly info files in the list
            foreach (string assInfoFile in assInfoFiles)
            {
                // does this file exist?
                if (!File.Exists(assInfoFile))
                {
                    // assembly info file does not exist
                    return WriteConsoleInfo("Specified file does not exist: '" + assInfoFile + "'", true);
                }
                Encoding assInfoFileEncoding = GetFileEncoding(assInfoFile);
                DateTime lastWriteTime = File.GetLastWriteTime(assInfoFile);
                FileAttributes assInfoAttributes = File.GetAttributes(assInfoFile);
                // get all of the lines in the assembly info file
                WriteConsoleInfo("Processing '" + assInfoFile + "'...", false);
                string[] assInfoFileLines = File.ReadAllLines(assInfoFile);
                // if there are no lines in the file - completely ignore it - no processing of it at all
                if (assInfoFileLines.Length > 0)
                {
                    Boolean FoundFileVersion = false;
                    Boolean FoundCopyright = false;
                    Boolean FoundCompany = false;
                    Boolean FoundProduct = false;
                    Boolean FoundVersion = false;
                    Boolean modifiedAssemblyInfoFile = false;
                    Boolean modifiedAssemblyInfoAttribReadOnly = false;
                    // loop through all of the lines in the assemblyInfo file
                    // looking for the line of interest
                    for (int i = 0; i < assInfoFileLines.Length; i++)
                    {
                        // get the line to work with
                        string line = assInfoFileLines[i];
                        if (line.Length > 2)
                        {
                            // ignore the line if it is a comment line (VB or C# or C++)
                            if (!(line.Substring(0, 1) == "'") && !(line.Substring(0, 2) == "//"))
                            {
                                // look for the assembly lines
                                if (line.Contains("AssemblyFileVersion"))
                                {
                                    FoundFileVersion = true;
                                    // get the current version from the file
                                    string currentVersion = line.Substring(line.IndexOf(Convert.ToChar(34)) + 1);
                                    currentVersion = currentVersion.Substring(0, currentVersion.IndexOf(Convert.ToChar(34)));
                                    string[] versionParts = currentVersion.Split(new char[] { '.' });

                                    // build the new version string
                                    fullNewVersionString = string.Empty;

                                    // was the full new version passed in, or were the parts passed?
                                    if (fullNewVersion != null)
                                    {
                                        // use the full new version string
                                        fullNewVersionString = fullNewVersion.ToString(4);
                                    }
                                    else
                                    {
                                        // handle major version
                                        if (versionMajor.Length == 0)
                                        {
                                            fullNewVersionString = versionParts[0] + ".";
                                        }
                                        else
                                        {
                                            fullNewVersionString = versionMajor + ".";
                                        }
                                        // handle minor version
                                        if (versionMinor.Length == 0)
                                        {
                                            fullNewVersionString += versionParts[1] + ".";
                                        }
                                        else
                                        {
                                            fullNewVersionString += versionMinor + ".";
                                        }
                                        // handle build version
                                        if (versionBuild.Length == 0)
                                        {
                                            fullNewVersionString += versionParts[2] + ".";
                                        }
                                        else
                                        {
                                            fullNewVersionString += versionBuild + ".";
                                        }
                                        // handle revision number
                                        if (versionRevison.Length == 0)
                                        {
                                            fullNewVersionString += versionParts[3];
                                        }
                                        else
                                        {
                                            fullNewVersionString += versionRevison;
                                        }
                                    }

                                    // write the version info back to the array
                                    if (currentVersion != fullNewVersionString)
                                    {
                                        modifiedAssemblyInfoFile = true;
                                        assInfoFileLines[i] = assInfoFileLines[i].Replace(currentVersion, fullNewVersionString);
                                    }
                                }
                                else if (line.Contains("AssemblyCompany"))
                                {
                                    FoundCompany = true;
                                    // handle company
                                    string oldCompany = string.Empty;
                                    if (newCompany.Length > 0)
                                    {
                                        // get the old company
                                        oldCompany = line.Substring(line.IndexOf(Convert.ToChar(34)) + 1);
                                        oldCompany = oldCompany.Substring(0, oldCompany.IndexOf(Convert.ToChar(34)));
                                        if (oldCompany != newCompany)
                                        {
                                            modifiedAssemblyInfoFile = true;
                                            // replace the old company with the new company
                                            assInfoFileLines[i] = assInfoFileLines[i].Replace('"'+oldCompany+'"', '"'+newCompany+'"');
                                        }
                                    }
                                }
                                else if (line.Contains("AssemblyCopyright"))
                                {
                                    FoundCopyright = true;
                                    // handle copyright
                                    string oldCopyright = string.Empty;
                                    if (newCopyright.Length > 0)
                                    {
                                        // get the old copyright
                                        oldCopyright = line.Substring(line.IndexOf(Convert.ToChar(34)) + 1);
                                        oldCopyright = oldCopyright.Substring(0, oldCopyright.IndexOf(Convert.ToChar(34)));
                                        if (oldCopyright != newCopyright)
                                        {
                                            modifiedAssemblyInfoFile = true;
                                            // replace the old copyright with the new copyright
                                            assInfoFileLines[i] = assInfoFileLines[i].Replace('"'+oldCopyright+'"', '"'+newCopyright+'"');
                                        }
                                    }
                                }
                                else if (line.Contains("AssemblyProduct"))
                                {
                                    FoundProduct = true;
                                    // handle product
                                    string oldProduct = string.Empty;
                                    if (newProduct.Length > 0)
                                    {
                                        // get the old product
                                        oldProduct = line.Substring(line.IndexOf(Convert.ToChar(34)) + 1);
                                        oldProduct = oldProduct.Substring(0, oldProduct.IndexOf(Convert.ToChar(34)));
                                        if (oldProduct != newProduct)
                                        {
                                            modifiedAssemblyInfoFile = true;
                                            // replace the old product with the new product
                                            assInfoFileLines[i] = assInfoFileLines[i].Replace('"'+oldProduct+'"', '"'+newProduct+'"');
                                        }
                                    }
                                }
                                else if (line.Contains("AssemblyVersion"))
                                {
                                    FoundVersion = true;
                                    // get the current version from the file
                                    string currentVersion = line.Substring(line.IndexOf(Convert.ToChar(34)) + 1);
                                    currentVersion = currentVersion.Substring(0, currentVersion.IndexOf(Convert.ToChar(34)));
                                    string[] versionParts = currentVersion.Split(new char[] { '.' });

                                    // build the new version string
                                    string newAssemblyVersionString = string.Empty;

                                    // was the full new version passed in, or were the parts passed?
                                    if (fullNewVersion != null)
                                    {
                                        // use the full new version string
                                        if (forceFullAssemblyVersion == true)
                                        {
                                            // set the AssemblyVersion to the 4-part value (override)
                                            newAssemblyVersionString = fullNewVersion.ToString(4);
                                        }
                                        else
                                        {
                                            // set the AssemblyVerion to the 2-part major.minor only (default)
                                            newAssemblyVersionString = fullNewVersion.ToString(2) + ".0.0";
                                        }
                                    }
                                    else
                                    {
                                        // handle major version
                                        if (versionMajor.Length == 0)
                                        {
                                            newAssemblyVersionString = versionParts[0] + ".";
                                        }
                                        else
                                        {
                                            newAssemblyVersionString = versionMajor + ".";
                                        }
                                        // handle minor version
                                        if (versionMinor.Length == 0)
                                        {
                                            newAssemblyVersionString += versionParts[1] + ".";
                                        }
                                        else
                                        {
                                            newAssemblyVersionString += versionMinor + ".";
                                        }
                                        if (forceFullAssemblyVersion == true)
                                        {
                                            // handle build version
                                            if (versionBuild.Length == 0)
                                            {
                                                newAssemblyVersionString += versionParts[2] + ".";
                                            }
                                            else
                                            {
                                                newAssemblyVersionString += versionBuild + ".";
                                            }
                                            // handle revision number
                                            if (versionRevison.Length == 0)
                                            {
                                                newAssemblyVersionString += versionParts[3];
                                            }
                                            else
                                            {
                                                newAssemblyVersionString += versionRevison;
                                            }
                                        }
                                        else
                                        {
                                            // handle build version
                                            newAssemblyVersionString += "0.";
                                            // handle revision number
                                            newAssemblyVersionString += "0";
                                        }
                                    }
                                    if (currentVersion != newAssemblyVersionString)
                                    {
                                        modifiedAssemblyInfoFile = true;
                                        // write the version info back to the array
                                        assInfoFileLines[i] = assInfoFileLines[i].Replace(currentVersion, newAssemblyVersionString);
                                    }
                                }   
                            }
                        }
                    }
                    // if the file needs to be updated, make sure it is writable
                    if ((modifiedAssemblyInfoFile == true) || (FoundCompany != true) || (FoundCopyright != true) || (FoundFileVersion != true) || (FoundProduct != true) || (FoundVersion != true))
                    {
                        if ((assInfoAttributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                        {
                            File.SetAttributes(assInfoFile, assInfoAttributes & ~FileAttributes.ReadOnly);
                            modifiedAssemblyInfoAttribReadOnly = true;
                        }
                    }
                    if (modifiedAssemblyInfoFile == true)
                    {
                        // write the updated lines back to the assembly info file
                        File.WriteAllLines(assInfoFile, assInfoFileLines, assInfoFileEncoding);
                    }

                    // deal with any missing lines
                    if (createMissingLines == true)
                    {
                        if (FoundFileVersion != true)
                        {
                            if (fullNewVersion != null)
                            {
                                fullNewVersionString = fullNewVersion.ToString(4);
                            }
                            else
                            {
                                if (versionMajor.Length == 0)
                                {
                                    fullNewVersionString = "0.";
                                }
                                else
                                {
                                    fullNewVersionString = versionMajor + ".";
                                }
                                if (versionMinor.Length == 0)
                                {
                                    fullNewVersionString += "0.";
                                }
                                else
                                {
                                    fullNewVersionString += versionMinor + ".";
                                }
                                if (versionBuild.Length == 0)
                                {
                                    fullNewVersionString += "0.";
                                }
                                else
                                {
                                    fullNewVersionString += versionBuild + ".";
                                }
                                if (versionRevison.Length == 0)
                                {
                                    fullNewVersionString += "0";
                                }
                                else
                                {
                                    fullNewVersionString += versionRevison;
                                }
                            }
                            // create and append a FileVersion line here
                            if (assInfoFile.ToLower().EndsWith(".cpp"))
                            {
                                File.AppendAllText(assInfoFile, "[assembly:AssemblyFileVersionAttribute(" + '"' + fullNewVersionString + '"' + ")];" + Environment.NewLine, assInfoFileEncoding);
                                modifiedAssemblyInfoFile = true;
                            }
                            else if (assInfoFile.ToLower().EndsWith(".vb"))
                            {
                                File.AppendAllText(assInfoFile, "<assembly: AssemblyFileVersion(" + '"' + fullNewVersionString + '"' + ")>" + Environment.NewLine, assInfoFileEncoding);
                                modifiedAssemblyInfoFile = true;
                            }
                            else if (assInfoFile.ToLower().EndsWith(".cs"))
                            {
                                File.AppendAllText(assInfoFile, "[assembly: AssemblyFileVersion(" + '"' + fullNewVersionString + '"' + ")]" + Environment.NewLine, assInfoFileEncoding);
                                modifiedAssemblyInfoFile = true;
                            }
                        }
                        if ((FoundCompany != true) && (newCompany.Length > 0))
                        {
                            // create and append a Company line here
                            if (assInfoFile.ToLower().EndsWith(".cpp"))
                            {
                                File.AppendAllText(assInfoFile, "[assembly:AssemblyCompanyAttribute(" + '"' + newCompany + '"' + ")];" + Environment.NewLine, assInfoFileEncoding);
                                modifiedAssemblyInfoFile = true;
                            }
                            else if (assInfoFile.ToLower().EndsWith(".vb"))
                            {
                                File.AppendAllText(assInfoFile, "<assembly: AssemblyCompany(" + '"' + newCompany + '"' + ")>" + Environment.NewLine, assInfoFileEncoding);
                                modifiedAssemblyInfoFile = true;
                            }
                            else if (assInfoFile.ToLower().EndsWith(".cs"))
                            {
                                File.AppendAllText(assInfoFile, "[assembly: AssemblyCompany(" + '"' + newCompany + '"' + ")]" + Environment.NewLine, assInfoFileEncoding);
                                modifiedAssemblyInfoFile = true;
                            }
                        }
                        if ((FoundCopyright != true) && (newCopyright.Length > 0))
                        {
                            // create and append a Copyright line here
                            if (assInfoFile.ToLower().EndsWith(".cpp"))
                            {
                                File.AppendAllText(assInfoFile, "[assembly:AssemblyCopyrightAttribute(" + '"' + newCopyright + '"' + ")];" + Environment.NewLine, assInfoFileEncoding);
                                modifiedAssemblyInfoFile = true;
                            }
                            else if (assInfoFile.ToLower().EndsWith(".vb"))
                            {
                                File.AppendAllText(assInfoFile, "<assembly: AssemblyCopyright(" + '"' + newCopyright + '"' + ")>" + Environment.NewLine, assInfoFileEncoding);
                                modifiedAssemblyInfoFile = true;
                            }
                            else if (assInfoFile.ToLower().EndsWith(".cs"))
                            {
                                File.AppendAllText(assInfoFile, "[assembly: AssemblyCopyright(" + '"' + newCopyright + '"' + ")]" + Environment.NewLine, assInfoFileEncoding);
                                modifiedAssemblyInfoFile = true;
                            }
                        }
                        if ((FoundProduct != true) && (newProduct.Length > 0))
                        {
                            // create and append a Product line here
                            if (assInfoFile.ToLower().EndsWith(".cpp"))
                            {
                                File.AppendAllText(assInfoFile, "[assembly:AssemblyProductAttribute(" + '"' + newProduct + '"' + ")];" + Environment.NewLine, assInfoFileEncoding);
                                modifiedAssemblyInfoFile = true;
                            }
                            else if (assInfoFile.ToLower().EndsWith(".vb"))
                            {
                                File.AppendAllText(assInfoFile, "<assembly: AssemblyProduct(" + '"' + newProduct + '"' + ")>" + Environment.NewLine, assInfoFileEncoding);
                                modifiedAssemblyInfoFile = true;
                            }
                            else if (assInfoFile.ToLower().EndsWith(".cs"))
                            {
                                File.AppendAllText(assInfoFile, "[assembly: AssemblyProduct(" + '"' + newProduct + '"' + ")]" + Environment.NewLine, assInfoFileEncoding);
                                modifiedAssemblyInfoFile = true;
                            }
                        }
                        if (FoundVersion != true)
                        {
                            string newAssemblyVersionString = string.Empty;
                            if (fullNewVersion != null)
                            {
                                if (forceFullAssemblyVersion == true)
                                {
                                    // set the AssemblyVersion to the 4-part value (override)
                                    newAssemblyVersionString = fullNewVersion.ToString(4);
                                }
                                else
                                {
                                    // set the AssemblyVerion to the 2-part major.minor only (default)
                                    newAssemblyVersionString = fullNewVersion.ToString(2) + ".0.0";
                                }
                            }
                            else
                            {
                                // handle major version
                                if (versionMajor.Length == 0)
                                {
                                    newAssemblyVersionString = "0" + ".";
                                }
                                else
                                {
                                    newAssemblyVersionString = versionMajor + ".";
                                }
                                // handle minor version
                                if (versionMinor.Length == 0)
                                {
                                    newAssemblyVersionString += "0" + ".";
                                }
                                else
                                {
                                    newAssemblyVersionString += versionMinor + ".";
                                }
                                if (forceFullAssemblyVersion == true)
                                {
                                    // handle build version
                                    if (versionBuild.Length == 0)
                                    {
                                        newAssemblyVersionString += "0" + ".";
                                    }
                                    else
                                    {
                                        newAssemblyVersionString += versionBuild + ".";
                                    }
                                    // handle revision number
                                    if (versionRevison.Length == 0)
                                    {
                                        newAssemblyVersionString += "0";
                                    }
                                    else
                                    {
                                        newAssemblyVersionString += versionRevison;
                                    }
                                }
                                else
                                {
                                    // handle build version and revision number
                                    newAssemblyVersionString += "0.0";
                                }
                            }
                            if (newAssemblyVersionString.Length > 0)
                            {
                                // create and append an AssemblyVersion line here
                                if (assInfoFile.ToLower().EndsWith(".cpp"))
                                {
                                    File.AppendAllText(assInfoFile, "[assembly:AssemblyVersionAttribute(" + '"' + newAssemblyVersionString + '"' + ")];" + Environment.NewLine, assInfoFileEncoding);
                                    modifiedAssemblyInfoFile = true;
                                }
                                else if (assInfoFile.ToLower().EndsWith(".vb"))
                                {
                                    File.AppendAllText(assInfoFile, "<assembly: AssemblyVersion(" + '"' + newAssemblyVersionString + '"' + ")>" + Environment.NewLine, assInfoFileEncoding);
                                    modifiedAssemblyInfoFile = true;
                                }
                                else if (assInfoFile.ToLower().EndsWith(".cs"))
                                {
                                    File.AppendAllText(assInfoFile, "[assembly: AssemblyVersion(" + '"' + newAssemblyVersionString + '"' + ")]" + Environment.NewLine, assInfoFileEncoding);
                                    modifiedAssemblyInfoFile = true;
                                }
                            }
                        }
                    }
                    if (modifiedAssemblyInfoFile == true)
                    {
                        // reset the last modified timestamp so the file does NOT appear to have been modified
                        // and thus Visual Studio won't rebuild the project just because of this file revision.
                        File.SetLastWriteTime(assInfoFile, lastWriteTime);
                        if (modifiedAssemblyInfoAttribReadOnly == true)
                        {
                            // reset the original file attributes (including ReadOnly), so this program doesn't introduce an issue with the next TFS get
                            File.SetAttributes(assInfoFile, assInfoAttributes);
                        }
                    }
                }
            }
            return WriteConsoleInfo("Done.", false); ;
        }

        public static bool CmdLineParmExists(string parmName, string[] args)
        {
            // loop through the array looking for the matching parm
            foreach (string parm in args)
            {
                // this one match?
                if ((parm.Length >= parmName.Length) &&
                    (parmName.ToLower() == parm.Substring(0, parmName.Length).ToLower()))
                {
                    // return true
                    return true;
                }
            }
            // return false
            return false;
        }

        public static string GetCmdLineParmValue(string parmName, string[] args)
        {
            // loop through the array looking for the matching parm
            foreach (string parm in args)
            {
                // this one match?
                if ((parm.Length >= parmName.Length) &&
                    (parmName.ToLower() == parm.Substring(0, parmName.Length).ToLower()))
                {
                    // this was the one we were looking for
                    if (parm.Length > parmName.Length)
                    {
                        // return the value
                        return parm.Substring(parmName.Length);
                    }
                    else
                    {
                        // nothing to return
                        return String.Empty;
                    }
                }
            }
            return String.Empty;
        }

        private static int WriteConsoleInfo(string info, bool isStopError)
        {
            // write the info string to the console
            Console.WriteLine("AsmblyInfo[" + DateTime.Now.ToString() + "]: " + info);

            // is this a stop error?
            if (isStopError)
            {
                // stop execution
                Console.WriteLine("AsmblyInfo[" + DateTime.Now.ToString() + "]: AsmblyInfo failed.");
                //Console.WriteLine("AsmblyInfo[" + DateTime.Now.ToString() + "]: Press 'Enter' to exit AsmblyInfo");
                //Console.ReadLine();
            }

            // return 1 for failure, 0 for success
            return ((isStopError) ? 1 : 0);
        }

        public static Encoding GetFileEncoding(String FileName)

        // Return the Encoding of a text file.  Return Encoding.Default if no Unicode
        // BOM (byte order mark) is found.
        {
            Encoding Result = null;

            FileInfo FI = new FileInfo(FileName);

            FileStream FS = null;

            try
            {
                FS = FI.OpenRead();

                Encoding[] UnicodeEncodings = { Encoding.BigEndianUnicode, Encoding.Unicode, Encoding.UTF8 };

                for (int i = 0; Result == null && i < UnicodeEncodings.Length; i++)
                {
                    FS.Position = 0;

                    byte[] Preamble = UnicodeEncodings[i].GetPreamble();

                    bool PreamblesAreEqual = true;

                    for (int j = 0; PreamblesAreEqual && j < Preamble.Length; j++)
                    {
                        PreamblesAreEqual = Preamble[j] == FS.ReadByte();
                    }

                    if (PreamblesAreEqual)
                    {
                        Result = UnicodeEncodings[i];
                    }
                }
            }
            catch (System.IO.IOException)
            {
            }
            finally
            {
                if (FS != null)
                {
                    FS.Close();
                }
            }

            if (Result == null)
            {
                Result = Encoding.Default;
            }

            return Result;
        }
    }
}
