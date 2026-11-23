using KifuwarabeGoPolypropylene.ThinkEngine;
if (args.Contains("--self-test")) SelfTest.Run();
else new GtpEngine().Run(Console.In, Console.Out);
