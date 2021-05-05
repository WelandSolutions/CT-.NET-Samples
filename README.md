# Compact Talk .NET Samples

## Overview

Sample projects using Compact Talk CTClient (WCF) interface.

## MiniWMS

Includes a .NET5 Windows Forms project implementing a mini WMS (MiniWMS). The project uses the CTClient WCF interface provided by Compact Talk.
In the project you will find examples on how to send and acknowledge orders and also how to receive and act on events sent from Compact Talk

The main logic for communicating with Compact Talk is implemented in class Weland.Ct.Api.Sample.MiniWMS.Services.OrderService

Compact Talk must be started before starting MiniWMS
