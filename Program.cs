using System.Reflection.PortableExecutable;
using CoordinateSharp;

namespace IlluminationScheduleGen
{
	internal class Program
	{
		static void Main(string[] args)
		{
			var tasmotaHost = "192.168.88.26";

			var scheduleStart = new DateTimeOffset(DateTime.Today);

			var timeOffset = DateTimeOffset.Now.Offset;

			var morningSwitchOnTime = TimeSpan.FromHours(6.5);
			var eveningSwitchOffTime = TimeSpan.FromHours(23.5);

			var weekendMorningSwitchOnTime = TimeSpan.FromHours(8.0);
			var weekendEveningSwitchOffTime = TimeSpan.FromHours(24.5);

			var sunriseShift = TimeSpan.FromMinutes(-20);
			var sunsetShift = TimeSpan.FromMinutes(0);

			// format: MM-DD, at these days, at evening illumination will not be switched off
			var wholeNightIlluminationDaysDefault = true;
			var wholeNightIlluminationDays = new List<string>
			{
				"12-31"
			};

			for (var i = 0;i < args.Length; i++)
			{
				if (args[i] == "--start")
				{
					scheduleStart = DateTimeOffset.Parse(args[++i]).LocalDateTime.Date;
				}
				else if(args[i] == "--tasmota-host")
				{
					tasmotaHost = args[++i];
				}
				else if (args[i] == "--offset")
				{
					timeOffset = TimeSpan.FromHours(int.Parse(args[++i]));
				}
				else if (args[i] == "--sunrise-shift")
				{
					sunriseShift = TimeSpan.FromMinutes(int.Parse(args[++i]));
				}
				else if (args[i] == "--sunset-shift")
				{
					sunsetShift = TimeSpan.FromMinutes(int.Parse(args[++i]));
				}
				else if (args[i] == "--morning-switch-on")
				{
					morningSwitchOnTime = TimeSpan.FromHours(double.Parse(args[++i]));
				}
				else if (args[i] == "--weekend-morning-switch-on")
				{
					weekendMorningSwitchOnTime = TimeSpan.FromHours(double.Parse(args[++i]));
				}
				else if (args[i] == "--evening-switch-off")
				{
					eveningSwitchOffTime = TimeSpan.FromHours(double.Parse(args[++i]));
				}
				else if (args[i] == "--weekend-evening-switch-off")
				{
					weekendEveningSwitchOffTime = TimeSpan.FromHours(double.Parse(args[++i]));
				}
				else if (args[i] == "--whole-day-illumination")
				{
					if (wholeNightIlluminationDaysDefault)
					{
						wholeNightIlluminationDaysDefault = false;
						wholeNightIlluminationDays = new List<string>();
					}
					wholeNightIlluminationDays.Add(args[++i]);
				}
			}

			Console.WriteLine($"# light on/off schedule from {scheduleStart}.");
			Console.WriteLine($"# time offset: {timeOffset}");
			Console.WriteLine($"# morning switch on: {morningSwitchOnTime} (weekend: {weekendMorningSwitchOnTime})");
			Console.WriteLine($"# evening switch off: {eveningSwitchOffTime} (weekend: {weekendEveningSwitchOffTime})");
			Console.WriteLine();

			var switchOnCmd = $"curl http://{tasmotaHost}/cm?cmnd=Power\\%20ON";
			var switchOffCmd = $"curl http://{tasmotaHost}/cm?cmnd=Power\\%20OFF";

			var day4Calculate = scheduleStart;
			var stopCondition = new HashSet<string>();

			while (true)
			{
				var stopkey = $"{day4Calculate.Month:D2}-{day4Calculate.Day:D2}";

				if (stopCondition.Contains(stopkey))
					break;

				stopCondition.Add(stopkey);

				var cel = Celestial.CalculateCelestialTimes(53.893009, 27.567444, day4Calculate.LocalDateTime, timeOffset.Hours);

				var sunrise = cel.SunRise.Value.TimeOfDay + sunriseShift;
				var sunset = cel.SunSet.Value.TimeOfDay + sunsetShift;

				var day = new DateTimeOffset(day4Calculate.Year, day4Calculate.Month, day4Calculate.Day, 0, 0, 0, timeOffset);

				Console.WriteLine($"# {day:dd MMM yyyy, ddd}");

				var isMorningWeekend = day.DayOfWeek == DayOfWeek.Saturday || day.DayOfWeek == DayOfWeek.Sunday;
				var morningSwitchOn = day + (isMorningWeekend ? weekendMorningSwitchOnTime : morningSwitchOnTime);
				var morningSwitchOff = day + sunrise;

				var isEveningWeekend = day.DayOfWeek == DayOfWeek.Friday || day.DayOfWeek == DayOfWeek.Saturday;
				var eveningSwitchOn = day + sunset;
				var eveningSwitchOff = day + (isEveningWeekend ? weekendEveningSwitchOffTime : eveningSwitchOffTime);

				if (morningSwitchOff - morningSwitchOn > TimeSpan.Zero)
				{
					Console.WriteLine($"{morningSwitchOn.Minute} {morningSwitchOn.Hour} {morningSwitchOn.Day} {morningSwitchOn.Month} * {switchOnCmd}");
					Console.WriteLine($"{morningSwitchOff.Minute} {morningSwitchOff.Hour} {morningSwitchOff.Day} {morningSwitchOff.Month} * {switchOffCmd}");
				}
				else
				{
					Console.WriteLine("# No morning illumination");
				}

				if (eveningSwitchOff - eveningSwitchOn > TimeSpan.Zero)
				{
					Console.WriteLine($"{eveningSwitchOn.Minute} {eveningSwitchOn.Hour} {eveningSwitchOn.Day} {eveningSwitchOn.Month} * {switchOnCmd}");

					if (!wholeNightIlluminationDays.Contains($"{day.Month:D2}-{day.Day:D2}"))
					{
						Console.WriteLine($"{eveningSwitchOff.Minute} {eveningSwitchOff.Hour} {eveningSwitchOff.Day} {eveningSwitchOff.Month} * {switchOffCmd}");
					}
				}
				else
				{
					Console.WriteLine("# No evening illumination");
				}

				Console.WriteLine("");

				day4Calculate += TimeSpan.FromDays(1);
			}
		}
	}
}