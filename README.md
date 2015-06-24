Windows 10 IoT Core and IFTTT
-----------------------------

As you already know [IFTTT](https://ifttt.com/) has release [Maker Channel](http://blog.ifttt.com/post/121786069098/introducing-the-maker-channel) recently. Maker Channel allows you to connect IFTTT and any third party service/applications. You make a POST or GET to the URL:

**`https://maker.ifttt.com/trigger/{event-name}/with/key/{secret-key}`**

Where *`event-name`* is the name of the event to trigger and *`secret-key`* is the secret assigned to your account. Using this Channel you can make any application/device to work with IFTTT. You can also post JSON body with variables *`value1`*, *`value2`* or *`value3`*. These values you can retrieve from the Channel variables. 

Last week I though of trying this with Windows 10 IoT Core. Here is a project I done last week. This project is an extension of one of my previous project with [Windows 10 IoT Core and TSL2561](http://www.hackster.io/krvarma/windows-10-iot-core-tsl2561). In this project, the system monitor the current Luminosity using TSL2561 sensor and when it goes below a minimum limit or goes higher than a maximum limit it sends a IFTTT event. I have configured IFTTT to send iOS Notification when ever it receives the Maker Channel events *`rpievent-low/rpievent-high/rpievent-normal`*  events. 

**Screenshots**

**Demo Video**
