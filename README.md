# Usage

    Usage: UnitTestToUML [options]
 
    Options:<br>
      -?|-h|--help                      Show help information
      -a|--apple-path <APPLE_PATH>      The path to the folder to read Apple unit tests from
      -c|--config-path <CONFIG_PATH>    The path to the file to read the program configuration from
      -s|--csharp-path <CSHARP_PATH>    The path to the folder to read C# unit tests from
      -j|--java-path <JAVA_PATH>        The path to the folder to read Java unit tests from
      -u|--umldirectory <UML_DIRECTORY> The path to the directory to write UML output to
      -v|--verbose                      Also write per platform unit test lists
  
# Notes

A sample config file can be found at the root of this repo (config.json)

This will generate up to four files in the specified UML directory:

1. diff.puml: The list of unit tests missing from each platform
2. csharp.puml: The overall list of unit tests in .NET
3. java.puml: The overall list of unit tests in Java
4. apple.puml: The overall list of unit tests in Objective-C

These files are in the [PlantUML Class Diagram](http://plantuml.com/class-diagram) format and PlantUML can be used to generate a graphical representation.
