# IlluminationScheduleGen
Generate crontab file for switch on/off illumination for tasmota based device

Example of generated crontab

```
# light on/off schedule for 2023 year.
# time offset: 03:00:00
# morning switch on: 06:30:00 (weekend: 08:00:00)
# evening switch off: 23:30:00 (weekend: 1.00:30:00)

# 01 Jan 2023, Sun
0 8 1 1 * curl http://192.168.88.26/cm?cmnd=Power\%20ON
29 9 1 1 * curl http://192.168.88.26/cm?cmnd=Power\%20OFF
59 16 1 1 * curl http://192.168.88.26/cm?cmnd=Power\%20ON
30 0 2 1 * curl http://192.168.88.26/cm?cmnd=Power\%20OFF

# 02 Jan 2023, Mon
30 6 2 1 * curl http://192.168.88.26/cm?cmnd=Power\%20ON
29 9 2 1 * curl http://192.168.88.26/cm?cmnd=Power\%20OFF
0 17 2 1 * curl http://192.168.88.26/cm?cmnd=Power\%20ON
30 23 2 1 * curl http://192.168.88.26/cm?cmnd=Power\%20OFF
```
