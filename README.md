# Custom web templates
Firt of all, [LibreHardwareMonitor](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor) is an awesome project, and thanks for all the work you have put in it!.

I really missed an app that would allow me to display simple HTML dashboard with live refresh using any json data source. I've realized that a tool that I use daily exposes json data file with live sensor data.

Inspirations:
* [Aida64 remote LCD display](https://forums.aida64.com/topic/2636-remotesensor-lcd-for-smartphones-and-tablets/)

## Description

My idea is to allow [LibreHardwareMonitor](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor) to load custom html documents instead of serving pre-prepared (and really awesome) sansors three page. I also wanted to keep current behaviour. 

## Implementation
I've just changed in an ugly way how `HttpServer` executes `Process(HttpListenerContext context)` method, now it allows to load files from hard drive and serve to the user using http. Any custom html can be loaded.

After making ugly => ok I will create PR to origin project.

## Future

A place with many different dashboards created by community.


## Custom web templates
/bin/[ver]/templates/[template_name]
[http://localhost:8085/template/jarek](http://localhost:8085/template/jarek)
![temps HM](https://user-images.githubusercontent.com/179938/207020005-473bc526-d060-4e30-ac90-f53aad890eda.gif)
