# skelecaster

skelecaster reads skeleton frame data from an attached Kinect, serializes it into UDP packets, and sends it over the network.

## Prerequisites

  - Microsoft Kinect SDK

## Usage

    C:\> skelecaster [ip_address:port]

If no arguments are given, IP address/port will be read interactively from console. If no port is given, the default is 11011.

## Packet Format

Each packet starts with a header:

| Offset      | Contents                       |
|-------------|--------------------------------|
| 0x00        | #0x01 (fixed value)            |
| 0x01        | #0x01 (fixed value)            |
| 0x02        | #0x02 (fixed value)            |
| 0x03        | number of tracked bodies       |

Followed by a data block for each tracked body:

| Offset (relative to body block start) | Length | Contents                |
|---------------------------------------|--------|-------------------------|
| 0x00                                  | 8      | Body tracking ID        |
| 0x08                                  | 4      | Joint 0 (X)             |
| 0x0C                                  | 4      | Joint 0 (Y)             |
| 0x10                                  | 4      | Joint 0 (Z)             |
| (joint position data repeats for joints 0 - 24)                        |||
| 0x134                                 | 1      | Joint 0 status          |
| (joint status data repeats for joints 0 - 24)                          |||

### Notes

  - all data is little endian
  - joint position components are encoded as floats
  - joint numbers correspond to values of [JointType enumeration](https://docs.microsoft.com/en-us/previous-versions/windows/kinect/dn758662(v%3dieb.10))
  - joint status values correspond to values of [TrackingState enumeration](https://docs.microsoft.com/en-us/previous-versions/windows/kinect/dn758896(v%3dieb.10))

## License

MIT
