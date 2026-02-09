GnE

GnE (pronounced "Genie" - the Gender-Name Estimator) takes an incoming XLSX or CSV file and outputs an appended version with additional columns for Gender and Accuracy based upon incoming columns for First Name and Country of Origin along with a summarized analysis of results.

The Gender column will contain one of the following result indicators:

    "F" - Woman - The person is most likely a woman based upon the model data.
    "I" - Indeterminate - There is equal maximum likelihood that the person has two or more possible results of woman, man, or unknown.
    "M" - Man - The persion is most likely a man based upon the model data.
    "-" - Not Available - The combination of First Name and Country of Origin are not contained in the model data.
    "?" - Unknown - The person's gender is unknown based upon the model data.

The Accuracy column will contain the probability that the Gender is correct based upon the model expressed as a decimal between 0 and 1.

Usage:
  GnE <file> [options]

Arguments:
  <file>  The input file to process

Options:
  -f, --firstName, --firstNameColumn, --fn, --fnc <firstNameColumn> (REQUIRED)                 The name or index of the column containing the first name
  -c, --cc, --country, --countryColumn <countryColumn> (REQUIRED)                              The name or index of the column containing the two-character code for the country of origin
  --per, --person, --personColumn <personColumn>                                               The name or index of the column containing the unique person identifier.  If not supplied, the combination of first name and country code are assumed 
                                                                                               to be unique
  --dis, --disclosure, --discolsureColumn, --pat, --patent, --patentColumn <discolsureColumn>  The name or index of the column containing the unique disclosure / patent identifier
  -d, --data, --dataFile <dataFile>                                                            The path to the World Gender-Name Dictionary name-gender-code CSV file used to determine gender and accuracy.  It defaults to the version provided 
                                                                                               with the software.  See https://github.com/IES-platform/r4r_gender/tree/main/wgnd
  -o, --output <output>                                                                        The path to the output file.  It defaults to the name of the input file with "-appendend" before the extension
  -s, --summary <summary>                                                                      The path to the output summary file.  It defaults to the name of the input file with "-appendend.txt" in place of the extension.  This option only 
                                                                                               applies to CSV processing
  -n, --nh, --no-headers                                                                       Indicates that the input file does not have a header row
  --rows-to-skip, --skip <rows-to-skip>                                                        The number of input rows to skip.  It only applies to XLSX files and must be >= 0 [default: 0]
  --rows-to-trim, --trim <rows-to-trim>                                                        The number of input rows to trim from the end of the data set.  It only applies to XLSX files and must be >= 0 [default: 0]
  -w, --sheet, --worksheet <worksheet>                                                         Indicates the name or index of the worksheet to process.  It only applies to XLSX files.  By default, the active worksheet in the XLSX file is used
  --version                                                                                    Show version information
  -?, -h, --help                                                                               Show help and usage information

The MIT License (MIT)

Copyright © 2023-2026, Richardson Oliver Insights, LLC, All Rights Reserved

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
