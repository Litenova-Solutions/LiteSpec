using LiteSpec.Cli;

// Messages quote page text verbatim, so the console must write UTF-8 rather
// than the Windows code page, which turns a non-ASCII character into a control byte.
Console.OutputEncoding = new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

return CliEntry.Run(args);
