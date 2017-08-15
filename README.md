
# servicefabric-simpleIOT
Simple demonstration project to show how multiple microservices can work together and integrate with service bus 

Projects:
* QueueItemStorageSrv - Stateful service that stores items in a reliable dictionary

* QueueWorkerSrv - Stateless service that listens to a service bus queue and stores all of its items into the stateful storage service

* theMayorUI - WebUI that shows the statefully stored items in a webpage and exposes them via JSON WebAPI

* QueueworkerStateless - project contains the service fabric deployment settings 

