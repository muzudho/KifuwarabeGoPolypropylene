using KifuwarabeGoPolypropylene.ThinkEngine;
if (args.Contains("--self-test")) SelfTest.Run();
else if (args.Contains("--lesson-cycle")) Environment.ExitCode = LessonCycle.Run(args);
else new GtpEngine().Run(Console.In, Console.Out);
