using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Bluetooth.Background;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Storage.Streams;
using System.Runtime.InteropServices;
using Windows.Devices.Enumeration;
using System.Collections.Concurrent;

namespace BLEKeyboardMouse
{
    class MyBLEKeyboardMouse
    {
        

        static Guid GuidExternalReportReferenceDescriptor = new Guid("00002907-0000-1000-8000-00805F9B34FB");
        static Guid GuidReportReferenceDescriptor = new Guid("00002908-0000-1000-8000-00805F9B34FB");
        const byte ReportIdKeyboard = 0x01;
        const byte ReportIdMouse = 0x02;
        const byte ReportTypeInput = 0x01;
        const byte ReportTypeOutput = 0x02;
        const byte ReportTypeFeature = 0x03;

        delegate void HandleWriteRequest(GattWriteRequest request, DataReader reader);

        static GattLocalCharacteristic chrKeyboardInputReport;
        static GattLocalCharacteristic chrBootKeyboardInputReport;
        static GattLocalCharacteristic chrMouseInputReport;
        static GattLocalCharacteristic chrBootMouseInputReport;
        static GattServiceProvider serviceProvider = null;

        public static async void MyBTInformation() {
            var ba = await BluetoothAdapter.GetDefaultAsync();
            MyConsole.WriteLine($"IsLowEnergySupported: {ba.IsLowEnergySupported}");
            MyConsole.WriteLine($"AreLowEnergySecureConnectionsSupported: {ba.AreLowEnergySecureConnectionsSupported}");
            MyConsole.WriteLine($"IsPeripheralRoleSupported: {ba.IsPeripheralRoleSupported}");
            MyConsole.WriteLine($"BluetoothAddress: {ba.BluetoothAddress}");
            //var di = await DeviceInformation.CreateFromIdAsync(ba.DeviceId);
            var bi = BluetoothAdapter.GetDeviceSelector();
            //var wc = DeviceInformation.CreateWatcher(bi);//加了这个之后BluetoothLEDevice.FromIdAsync才不会慢到死，很迷
            //wc.Added += async (DeviceWatcher sender, DeviceInformation deviceInterface) => {
            //    Console.WriteLine($"wc.Added");
            //};
            //wc.EnumerationCompleted += async (DeviceWatcher sender, Object args) => {
            //    Console.WriteLine($"wc.EnumerationCompleted");
            //};

            //wc.Removed += async (DeviceWatcher sender, DeviceInformationUpdate devUpdate) => {
            //    Console.WriteLine($"wc.Removed");
            //};
            //wc.Stopped += async (DeviceWatcher sender, object args) => {
            //    Console.WriteLine($"wc.Stopped");
            //};
            //wc.Updated += async (DeviceWatcher sender, DeviceInformationUpdate devUpdate) => {
            //    Console.WriteLine($"wc.Updated");
            //};
            //wc.Start();

        }
        static SerialSLIP.SerialSLIP serialSLIP=null;
        public static void EnableUART(string portName, int baudrate) {
            serialSLIP = new SerialSLIP.SerialSLIP(portName, baudrate);
            serialSLIP.OnRecvPacket += SerialSLIP_OnRecvPacket;
            try {
                serialSLIP.Open();
                MyConsole.WriteLine($"UART '{portName}' open success!");
            } catch {
                MyConsole.WriteLine($"UART '{portName}' open failed!");
                serialSLIP = null;
            }
        }

        private static void SerialSLIP_OnRecvPacket(byte[] data, int len, bool isPassCheckSum) {
            if (isPassCheckSum) {
                //var s = System.Text.Encoding.UTF8.GetString(data, 0, len);
                for (int i = 0; i < len; i++) {
                    Console.Write(data[i]);
                    Console.Write(" ");
                }
                Console.WriteLine("");
            } else {
                Console.WriteLine("pc recv serialSLIP packet error!");
            }
            
        }

        public static async void BLERun() {
            /**
             A service declaration is an Attribute with the Attribute Type set to the UUID for
             «Primary Service» or «Secondary Service». The Attribute Value shall be the
             16-bit Bluetooth UUID or 128-bit UUID for the service, known as the service
             UUID. A client shall support the use of both 16-bit and 128-bit UUIDs. A client
             may ignore any service definition with an unknown service UUID. An unknown
             service UUID is a UUID for an unsupported service. The Attribute Permissions
             shall be read-only and shall not require authentication or authorization.
             */
            /**
             * create hid service
             */
            Guid uuid = GattServiceUuids.HumanInterfaceDevice;
            var result = await GattServiceProvider.CreateAsync(uuid);
            if (result.Error != BluetoothError.Success) {
                throw new Exception($"Create hID service failed: {result.Error}");
            }
             serviceProvider = result.ServiceProvider;
            /**
             * A characteristic declaration is an Attribute with the Attribute Type set to the UUID for «Characteristic» and 
             * Attribute Value set to the Characteristic Properties, Characteristic Value Attribute Handle and Characteristic UUID. 
             * The Attribute Permissions shall be readable and not require authentication or authorization.
             */
            /**
             * hid information
             */
            uuid = GattCharacteristicUuids.HidInformation;
            var chrParams = new GattLocalCharacteristicParameters {
                CharacteristicProperties = (GattCharacteristicProperties.Read),
                ReadProtectionLevel = GattProtectionLevel.EncryptionRequired,
                //前两个字节是HID版本，一般填入0x01，0x01，表示版本号为1.1
                //第三个字节是Country Code，一般填00（0x21表示美式键盘 ）
                //第四个字节是HID Flags，第一位表示是否可以唤醒主机，第二位表示hid设备处于已绑定但未连接时是否广播。(这个好像没啥效果，不是很懂)
                //StaticValue = (new byte[] { 0x01, 0x01, 0x00, 0x01 }).AsBuffer(),
            };
            var chrResult = await serviceProvider.Service.CreateCharacteristicAsync(uuid, chrParams);
            if (chrResult.Error != BluetoothError.Success) {
                throw new Exception($"Create hid device information chara failed: {chrResult.Error}");
            }
            chrResult.Characteristic.ReadRequested += delegate (GattLocalCharacteristic sender, GattReadRequestedEventArgs args) {
                MyConsole.WriteLine("ReadRequested : hid device information!");
                var bs = new byte[] { 0x01, 0x01, 0x00, 0x01 };
                ProcessReadRequest(sender, args, bs);
            };
            /**
             * hid control point
             * Informs HID Device that HID Host is entering(0x00) or exiting(0x01) the Suspend State
             */
            uuid = GattCharacteristicUuids.HidControlPoint;
            chrParams = new GattLocalCharacteristicParameters {
                CharacteristicProperties = (GattCharacteristicProperties.WriteWithoutResponse),
                WriteProtectionLevel = GattProtectionLevel.EncryptionRequired
            };
            chrResult = await serviceProvider.Service.CreateCharacteristicAsync(uuid, chrParams);
            if (chrResult.Error != BluetoothError.Success) {
                throw new Exception($"Create hid control point chara failed: {chrResult.Error}");
            }
            chrResult.Characteristic.WriteRequested += HidControlPoint_WriteRequested;

            /**
             * hid protocol mode 
             */
            uuid = GattCharacteristicUuids.ProtocolMode;
            chrParams = new GattLocalCharacteristicParameters {
                CharacteristicProperties = (GattCharacteristicProperties.Read | GattCharacteristicProperties.WriteWithoutResponse),
                ReadProtectionLevel = GattProtectionLevel.EncryptionRequired,
                WriteProtectionLevel = GattProtectionLevel.EncryptionRequired
            };
            chrResult = await serviceProvider.Service.CreateCharacteristicAsync(uuid, chrParams);
            if (chrResult.Error != BluetoothError.Success) {
                throw new Exception($"Create hid protocol mode chara failed: {chrResult.Error}");
            }
            chrResult.Characteristic.ReadRequested += ProtocolMode_ReadRequested;
            chrResult.Characteristic.WriteRequested += ProtocolMode_WriteRequested;

            /**
             * hid report map
             */
            uuid = GattCharacteristicUuids.ReportMap;
            chrParams = new GattLocalCharacteristicParameters {
                CharacteristicProperties = (GattCharacteristicProperties.Read),
                ReadProtectionLevel = GattProtectionLevel.EncryptionRequired,
                //StaticValue = 
            };
            chrResult = await serviceProvider.Service.CreateCharacteristicAsync(uuid, chrParams);
            if (chrResult.Error != BluetoothError.Success) {
                throw new Exception($"Create hid report map chara failed: {chrResult.Error}");
            }
            chrResult.Characteristic.ReadRequested += delegate (GattLocalCharacteristic sender, GattReadRequestedEventArgs args) {
                MyConsole.WriteLine("ReadRequested : report map!");
                var bs = new byte[] { 
                    /*Report Description: describes what we communicate*/
                    0x05, 0x01,     // Usage Pg (Generic Desktop)
                    0x09, 0x06,     // Usage (Keyboard)
                    0xA1, 0x01,     // Collection: (Application)
                    0x85, ReportIdKeyboard,     // REPORT_ID (1)
                    0x05, 0x07,     // Usage Pg (Key Codes)
                    0x19, 0xE0,     // Usage Min (224)
                    0x29, 0xE7,     // Usage Max (231)
                    0x15, 0x00,     // Log Min (0)
                    0x25, 0x01,     // Log Max (1)
                                    //
                                    // Modifier byte
                    0x75, 0x01,     // Report Size (1)
                    0x95, 0x08,     // Report Count (8)
                    0x81, 0x02,     // Input: (Data, Variable, Absolute)
                                    //
                                    // Reserved byte
                    0x95, 0x01,     // Report Count (1)
                    0x75, 0x08,     // Report Size (8)
                    0x81, 0x01,     // Input: (Constant)
                                    //
                                    // LED report
                    0x95, 0x05,     // Report Count (5)
                    0x75, 0x01,     // Report Size (1)
                    0x05, 0x08,     // Usage Pg (LEDs)
                    0x19, 0x01,     // Usage Min (1)
                    0x29, 0x05,     // Usage Max (5)
                    0x91, 0x02,     // Output: (Data, Variable, Absolute)
                                    //
                                    // LED report padding
                    0x95, 0x01,     // Report Count (1)
                    0x75, 0x03,     // Report Size (3)
                    0x91, 0x01,     // Output: (Constant)
                                    //
                                    // Key arrays (6 bytes)
                    0x95, 0x06,     // Report Count (6)
                    0x75, 0x08,     // Report Size (8)
                    0x15, 0x00,     // Log Min (0)
                    0x25, 0x65,     // Log Max (101)
                    0x05, 0x07,     // Usage Pg (Key Codes)
                    0x19, 0x00,     // Usage Min (0)
                    0x29, 0x65,     // Usage Max (101)
                    0x81, 0x00,     // Input: (Data, Array)
                                    //
                    0xC0,           // End Collection

                    0x05, 0x01,     // USAGE_PAGE (Generic Desktop)
                    0x09, 0x02,     // USAGE (Mouse)
                    0xa1, 0x01,     // COLLECTION (Application)
                    0x85, ReportIdMouse,     // REPORT_ID (2)
                    0x09, 0x01,     //   USAGE (Pointer)
                    0xa1, 0x00,     //   COLLECTION (Physical)
                    0x95, 0x03,     //     REPORT_COUNT (3)
                    0x75, 0x01,     //     REPORT_SIZE (1)
                    0x05, 0x09,     //     USAGE_PAGE (Button)
                    0x19, 0x01,     //     USAGE_MINIMUM (Button 1)
                    0x29, 0x03,     //     USAGE_MAXIMUM (Button 3)
                    0x25, 0x01,     //     LOGICAL_MAXIMUM (1)
                    0x81, 0x02,     //     INPUT (Data,Var,Abs)
                    0x95, 0x05,     //     REPORT_COUNT (5)
                    0x75, 0x01,     //     REPORT_SIZE (1)
                    0x15, 0x00,     //     LOGICAL_MINIMUM (0)
                    0x81, 0x01,     //     INPUT (Cnst,Ary,Abs)
                    0x95, 0x02,     //     REPORT_COUNT (3)
                    0x75, 0x08,     //     REPORT_SIZE (8)
                    0x05, 0x01,     //     USAGE_PAGE (Generic Desktop)
                    0x09, 0x30,     //     USAGE (X)
                    0x09, 0x31,     //     USAGE (Y)
                    0x09, 0x38,     //     USAGE (Wheel)
                    0x15, 0x81,     //     LOGICAL_MINIMUM (-127)
                    0x25, 0x7f,     //     LOGICAL_MAXIMUM (127)
                    //0x36, 0x99, 0xF3,// physical min -3175
                    //0x46, 0x67, 0x0C,// physical max 3175
                    //0x55, 0x0C,     //Unit Exponent -4
                    //0x65, 0x13,     //unit inches
                    0x81, 0x06,     //     INPUT (Data,Var,Rel)
                    0xc0,           //     END_COLLECTION
                    0xc0,            // END_COLLECTION
                };
                ProcessReadRequest(sender, args, bs);
                
            };
            /**
             * hid report (keyboard input report)
             */
            uuid = GattCharacteristicUuids.Report;
            chrParams = new GattLocalCharacteristicParameters {
                CharacteristicProperties = (GattCharacteristicProperties.Read | GattCharacteristicProperties.Notify),
                ReadProtectionLevel = GattProtectionLevel.EncryptionRequired,
            };
            chrResult = await serviceProvider.Service.CreateCharacteristicAsync(uuid, chrParams);
            if (chrResult.Error != BluetoothError.Success) {
                throw new Exception($"Create hid report chara(keyboard input report) failed: {chrResult.Error}");
            }
            //reference input report 
            var descResult=await chrResult.Characteristic.CreateDescriptorAsync(GuidReportReferenceDescriptor, new GattLocalDescriptorParameters {
                ReadProtectionLevel = GattProtectionLevel.Plain,
                //StaticValue = (new byte[] { ReportIdKeyboard, ReportTypeInput }).AsBuffer(),//{report id, report type}
            });
            if (descResult.Error != BluetoothError.Success) {
                throw new Exception($"Create keyboard input report reference descriptor failed: {descResult.Error}");
            }
            descResult.Descriptor.ReadRequested +=delegate(GattLocalDescriptor sender, GattReadRequestedEventArgs args) {
                MyConsole.WriteLine("ReadDescriptor : keyboard input report reference!");
                var bs = new byte[] { ReportIdKeyboard, ReportTypeInput };
                ProcessReadDescriptor(sender, args, bs);
            };
            chrResult.Characteristic.ReadRequested += InputReportKeyboard_ReadRequested;
            chrResult.Characteristic.SubscribedClientsChanged += InputReportKeyboard_SubscribedClientsChanged;
            chrKeyboardInputReport = chrResult.Characteristic;

            /**
             * hid report (keyboard output report)
             */
            uuid = GattCharacteristicUuids.Report;
            chrParams = new GattLocalCharacteristicParameters {
                CharacteristicProperties = (GattCharacteristicProperties.Read | GattCharacteristicProperties.Write | GattCharacteristicProperties.WriteWithoutResponse),
                ReadProtectionLevel = GattProtectionLevel.EncryptionRequired,
                WriteProtectionLevel = GattProtectionLevel.EncryptionRequired,
            };
            chrResult = await serviceProvider.Service.CreateCharacteristicAsync(uuid, chrParams);
            if (chrResult.Error != BluetoothError.Success) {
                throw new Exception($"Create hid report chara(keyboard output report) failed: {chrResult.Error}");
            }
            //reference output report 
            descResult=await chrResult.Characteristic.CreateDescriptorAsync(GuidReportReferenceDescriptor, new GattLocalDescriptorParameters {
                ReadProtectionLevel = GattProtectionLevel.Plain,
                //StaticValue = (new byte[] { ReportIdKeyboard, ReportTypeOutput }).AsBuffer(),//{report id, report type}
            });
            if (descResult.Error != BluetoothError.Success) {
                throw new Exception($"Create keyboard output report reference descriptor failed: {descResult.Error}");
            }
            descResult.Descriptor.ReadRequested += delegate (GattLocalDescriptor sender, GattReadRequestedEventArgs args) {
                MyConsole.WriteLine("ReadDescriptor : keyboard output report reference!");
                var bs = new byte[] { ReportIdKeyboard, ReportTypeOutput };
                ProcessReadDescriptor(sender, args, bs);
            };
            chrResult.Characteristic.ReadRequested += OutputReportKeyboard_ReadRequested;
            chrResult.Characteristic.WriteRequested += OutputReportKeyboard_WriteRequested;
            

            /**
             * hid report (mouse input report)
             */
            uuid = GattCharacteristicUuids.Report;
            chrParams = new GattLocalCharacteristicParameters {
                CharacteristicProperties = (GattCharacteristicProperties.Read | GattCharacteristicProperties.Notify),
                ReadProtectionLevel = GattProtectionLevel.EncryptionRequired,

            };
            chrResult = await serviceProvider.Service.CreateCharacteristicAsync(uuid, chrParams);
            if (chrResult.Error != BluetoothError.Success) {
                throw new Exception($"Create hid report chara(mouse input report) failed: {chrResult.Error}");
            }
            //reference input report 
            descResult = await chrResult.Characteristic.CreateDescriptorAsync(GuidReportReferenceDescriptor, new GattLocalDescriptorParameters {
                ReadProtectionLevel = GattProtectionLevel.Plain,
                //StaticValue = (new byte[] { ReportIdMouse, ReportTypeInput }).AsBuffer(),//{report id, report type}
            });
            if (descResult.Error != BluetoothError.Success) {
                throw new Exception($"Create mouse input report reference descriptor failed: {descResult.Error}");
            }
            descResult.Descriptor.ReadRequested += delegate (GattLocalDescriptor sender, GattReadRequestedEventArgs args) {
                MyConsole.WriteLine("ReadDescriptor : mouse input report reference!");
                var bs = new byte[] { ReportIdMouse, ReportTypeInput };
                ProcessReadDescriptor(sender, args, bs);
            };
            chrResult.Characteristic.ReadRequested += InputReportMouse_ReadRequested;
            chrResult.Characteristic.SubscribedClientsChanged += InputReportMouse_SubscribedClientsChanged;
            chrMouseInputReport = chrResult.Characteristic;

            /**
             * hid report (feature report)
             */
            uuid = GattCharacteristicUuids.Report;
            chrParams = new GattLocalCharacteristicParameters {
                CharacteristicProperties = (GattCharacteristicProperties.Read | GattCharacteristicProperties.Write),
                ReadProtectionLevel = GattProtectionLevel.EncryptionRequired,
                WriteProtectionLevel = GattProtectionLevel.EncryptionRequired,
            };
            chrResult = await serviceProvider.Service.CreateCharacteristicAsync(uuid, chrParams);
            if (chrResult.Error != BluetoothError.Success) {
                throw new Exception($"Create hid report chara(feature report) failed: {chrResult.Error}");
            }
            //reference input report 
            descResult=await chrResult.Characteristic.CreateDescriptorAsync(GuidReportReferenceDescriptor, new GattLocalDescriptorParameters {
                ReadProtectionLevel = GattProtectionLevel.Plain,
                //StaticValue = (new byte[] { ReportIdKeyboard, ReportTypeFeature }).AsBuffer(),//{report id, report type}
            });
            if (descResult.Error != BluetoothError.Success) {
                throw new Exception($"Create keyboard feature report reference descriptor failed: {descResult.Error}");
            }
            descResult.Descriptor.ReadRequested += delegate (GattLocalDescriptor sender, GattReadRequestedEventArgs args) {
                MyConsole.WriteLine("ReadDescriptor : keyboard feature report reference!");
                var bs = new byte[] { ReportIdKeyboard, ReportTypeFeature };
                ProcessReadDescriptor(sender, args, bs);
            };
            chrResult.Characteristic.ReadRequested += FeatureReport_ReadRequested; ;
            chrResult.Characteristic.WriteRequested += FeatureReport_WriteRequested;



            /**
             * Boot keyboard input report
             */
            uuid = GattCharacteristicUuids.BootKeyboardInputReport;
            chrParams = new GattLocalCharacteristicParameters {
                CharacteristicProperties = (GattCharacteristicProperties.Read | GattCharacteristicProperties.Notify),
                ReadProtectionLevel = GattProtectionLevel.EncryptionRequired,
            };
            chrResult = await serviceProvider.Service.CreateCharacteristicAsync(uuid, chrParams);
            if (chrResult.Error != BluetoothError.Success) {
                throw new Exception($"Create Boot keyboard input report chara failed: {chrResult.Error}");
            }
            chrResult.Characteristic.ReadRequested += BootKeyboardInputReport_ReadRequested;
            chrResult.Characteristic.SubscribedClientsChanged += BootKeyboardInputReport_SubscribedClientsChanged;
            chrBootKeyboardInputReport = chrResult.Characteristic;
            /**
             * Boot keyboard output report
             */
            uuid = GattCharacteristicUuids.BootKeyboardOutputReport;
            chrParams = new GattLocalCharacteristicParameters {
                CharacteristicProperties = (GattCharacteristicProperties.Read | GattCharacteristicProperties.Write | GattCharacteristicProperties.WriteWithoutResponse),
                ReadProtectionLevel = GattProtectionLevel.EncryptionRequired,
                WriteProtectionLevel = GattProtectionLevel.EncryptionRequired,
            };
            chrResult = await serviceProvider.Service.CreateCharacteristicAsync(uuid, chrParams);
            if (chrResult.Error != BluetoothError.Success) {
                throw new Exception($"Create Boot keyboard output report chara failed: {chrResult.Error}");
            }
            chrResult.Characteristic.WriteRequested += BootKeyboardOutputReport_WriteRequested;
            chrResult.Characteristic.ReadRequested += BootKeyboardOutputReport_ReadRequested;
            /**
             * Boot Mouse input report
             */
            uuid = GattCharacteristicUuids.BootMouseInputReport;

            chrParams = new GattLocalCharacteristicParameters {
                CharacteristicProperties = (GattCharacteristicProperties.Read | GattCharacteristicProperties.Notify),
                ReadProtectionLevel = GattProtectionLevel.EncryptionRequired,
            };
            chrResult = await serviceProvider.Service.CreateCharacteristicAsync(uuid, chrParams);
            if (chrResult.Error != BluetoothError.Success) {
                throw new Exception($"Create Boot Mouse input report chara failed: {chrResult.Error}");
            }
            chrResult.Characteristic.ReadRequested += BootMouseInputReport_ReadRequested;
            chrResult.Characteristic.SubscribedClientsChanged += BootMouseInputReport_SubscribedClientsChanged;
            chrBootMouseInputReport = chrResult.Characteristic;


            serviceProvider.AdvertisementStatusChanged += ServiceProvider_AdvertisementStatusChanged;

            serviceProvider.StartAdvertising(new GattServiceProviderAdvertisingParameters {
                IsConnectable = true,
                IsDiscoverable = true
            });

        }


        private static void ServiceProvider_AdvertisementStatusChanged(GattServiceProvider sender, GattServiceProviderAdvertisementStatusChangedEventArgs args) {
            // Created - The default state of the advertisement, before the service is published for the first time.
            // Stopped - Indicates that the application has canceled the service publication and its advertisement.
            // Started - Indicates that the system was successfully able to issue the advertisement request.
            // Aborted - Indicates that the system was unable to submit the advertisement request, or it was canceled due to resource contention.

            MyConsole.WriteLine($"New Advertisement Status: {sender.AdvertisementStatus}");
        }

        static byte protocolMode = 0x01;//default Report Protocol Mode
        private static void ProtocolMode_WriteRequested(GattLocalCharacteristic sender, GattWriteRequestedEventArgs args) {
            ProcessWriteRequest(sender, args, 1, delegate (GattWriteRequest request, DataReader reader) {
                byte val = reader.ReadByte();
                MyConsole.WriteLine($"ProtocolMode_WriteRequested: {val}");
                protocolMode = val;
            });

        }

        private static void ProtocolMode_ReadRequested(GattLocalCharacteristic sender, GattReadRequestedEventArgs args) {
            MyConsole.WriteLine($"ProtocolMode_ReadRequested!");
            ProcessReadRequest(sender, args, protocolMode);
        }



        private static void HidControlPoint_WriteRequested(GattLocalCharacteristic sender, GattWriteRequestedEventArgs args) {
            ProcessWriteRequest(sender, args, 1, delegate (GattWriteRequest request, DataReader reader) {

                byte val = reader.ReadByte();
                MyConsole.WriteLine($"HidControlPoint_WriteRequested: {val}");
                if (val == 0) {
                    MyConsole.WriteLine("Host entering suspend state...");
                } else if (val == 1) {
                    MyConsole.WriteLine("Host exting suspend state...");
                } else {
                    MyConsole.WriteLine("Host unknown state!");
                }

            });
        }

        static byte[] iKeyboardReports = new byte[8];
        static byte[] iMouseReports = new byte[8];
        static byte[] oReports = new byte[1];

        static ConcurrentDictionary<string, BluetoothLEDevice> clients = new ConcurrentDictionary<string, BluetoothLEDevice>();

        private static async void SubscribedClientsChanged(GattLocalCharacteristic sender) {
            for (int i = 0; i < sender.SubscribedClients.Count; i++) {
                var client = sender.SubscribedClients[i].Session;
                client.MaintainConnection = true;
                if (!clients.ContainsKey(client.DeviceId.Id)) {
                    Console.WriteLine(System.DateTime.Now);
                    var bleD = await BluetoothLEDevice.FromIdAsync(client.DeviceId.Id);//太慢了
                    Console.WriteLine(System.DateTime.Now);
                    if(clients.TryAdd(client.DeviceId.Id, bleD)) {
                        bleD.ConnectionStatusChanged += BleD_ConnectionStatusChanged;
                        MyConsole.WriteLine($"SubscribedClientsChanged: {bleD.Name}");
                    }
                }
            }
        }

        private static void BleD_ConnectionStatusChanged(BluetoothLEDevice sender, object args) {
            MyConsole.WriteLine($"BleD_ConnectionStatusChanged: {sender.Name} {sender.ConnectionStatus}");
            BluetoothLEDevice ble;
            if (sender.ConnectionStatus == BluetoothConnectionStatus.Disconnected) {
                clients.TryRemove(sender.DeviceId, out ble);
                sender.Dispose();
            } else {
                if(!clients.ContainsKey(sender.DeviceId))
                    clients.TryAdd(sender.DeviceId, sender);
            }
        }


        private static void BootMouseInputReport_SubscribedClientsChanged(GattLocalCharacteristic sender, object args) {
            SubscribedClientsChanged(sender);
            MyConsole.WriteLine("BootMouseInputReport_SubscribedClientsChanged");
        }

        private static void BootKeyboardInputReport_SubscribedClientsChanged(GattLocalCharacteristic sender, object args) {
            SubscribedClientsChanged(sender);
            MyConsole.WriteLine("BootKeyboardInputReport_SubscribedClientsChanged");
        }
        private static void InputReportMouse_SubscribedClientsChanged(GattLocalCharacteristic sender, object args) {
            SubscribedClientsChanged(sender);
            MyConsole.WriteLine("InputReportMouse_SubscribedClientsChanged");
        }

        private static void InputReportKeyboard_SubscribedClientsChanged(GattLocalCharacteristic sender, object args) {
            SubscribedClientsChanged(sender);
            MyConsole.WriteLine("InputReportKeyboard_SubscribedClientsChanged");
        }

        private static void InputReportMouse_ReadRequested(GattLocalCharacteristic sender, GattReadRequestedEventArgs args) {
            MyConsole.WriteLine($"InputReportMouse_ReadRequested!");
            ProcessReadRequest(sender, args, iMouseReports);
        }
        private static void BootMouseInputReport_ReadRequested(GattLocalCharacteristic sender, GattReadRequestedEventArgs args) {
            MyConsole.WriteLine($"BootMouseInputReport_ReadRequested!");
            ProcessReadRequest(sender, args, iMouseReports);

        }
        private static void BootKeyboardInputReport_ReadRequested(GattLocalCharacteristic sender, GattReadRequestedEventArgs args) {
            MyConsole.WriteLine($"BootKeyboardInputReport_ReadRequested!");
            ProcessReadRequest(sender, args, iKeyboardReports);
        }
        private static void InputReportKeyboard_ReadRequested(GattLocalCharacteristic sender, GattReadRequestedEventArgs args) {
            MyConsole.WriteLine($"InputReportKeyboard_ReadRequested!");
            ProcessReadRequest(sender, args, iKeyboardReports);
        }

        

        private static void OutputReportKeyboard_ReadRequested(GattLocalCharacteristic sender, GattReadRequestedEventArgs args) {
            oReports[0] =MyWin32Api.GetKeyboardLED();
            MyConsole.WriteLine($"OutputReportKeyboard_ReadRequested!");
            ProcessReadRequest(sender, args, oReports);
        }
        private static void BootKeyboardOutputReport_ReadRequested(GattLocalCharacteristic sender, GattReadRequestedEventArgs args) {
            oReports[0] = MyWin32Api.GetKeyboardLED();
            MyConsole.WriteLine($"BootKeyboardOutputReport_ReadRequested!");
            ProcessReadRequest(sender, args, oReports);
        }

        private static void OutputReportKeyboard_WriteRequested(GattLocalCharacteristic sender, GattWriteRequestedEventArgs args) {
            ProcessWriteRequest(sender, args, 1, delegate (GattWriteRequest request, DataReader reader) {
                byte val = reader.ReadByte();
                MyConsole.WriteLine($"OutputReportKeyboard_WriteRequested: 0b{Convert.ToString(val, 2)}");
                oReports[0] = val;
                MyWin32Api.SetKeyboardLED(oReports[0]);
            });
        }

        private static void BootKeyboardOutputReport_WriteRequested(GattLocalCharacteristic sender, GattWriteRequestedEventArgs args) {
            ProcessWriteRequest(sender, args, 1, delegate (GattWriteRequest request, DataReader reader) {
                byte val = reader.ReadByte();
                MyConsole.WriteLine($"BootKeyboardOutputReport_WriteRequested: 0b{Convert.ToString(val, 2)}");
                oReports[0] = val;
                MyWin32Api.SetKeyboardLED(oReports[0]);
            });
        }


        private static void FeatureReport_ReadRequested(GattLocalCharacteristic sender, GattReadRequestedEventArgs args) {
            throw new NotImplementedException();
        }

        private static void FeatureReport_WriteRequested(GattLocalCharacteristic sender, GattWriteRequestedEventArgs args) {
            throw new NotImplementedException();
        }

        private static void ProcessReadRequest(GattLocalCharacteristic sender, GattReadRequestedEventArgs args, byte report) {
            byte[] reports = { report };
            ProcessReadRequest(sender, args, reports);
        }

        private static async void ProcessReadRequest(GattLocalCharacteristic sender, GattReadRequestedEventArgs args, byte[] reports) {
            //The Protocol Mode characteristic value shall be reset to the default value
            //following connection establishment.
            using (args.GetDeferral()) {
                // Get the request information.  This requires device access before an app can access the device's request. 
                GattReadRequest request = await args.GetRequestAsync();
                if (request == null) {
                    // No access allowed to the device.  Application should indicate this to the user.
                    MyConsole.WriteLine("Access to device not allowed!");
                    return;
                }

                var writer = new DataWriter();
                writer.ByteOrder = ByteOrder.LittleEndian;
                writer.WriteBytes(reports);


                // Can get details about the request such as the size and offset, as well as monitor the state to see if it has been completed/cancelled externally.
                // request.Offset
                // request.Length
                // request.State
                // request.StateChanged += <Handler>

                // Gatt code to handle the response
                request.RespondWithValue(writer.DetachBuffer());
            }
        }


        private static async void ProcessReadDescriptor(GattLocalDescriptor sender, GattReadRequestedEventArgs args, byte[] reports) {
            using (args.GetDeferral()) {
                var request = await args.GetRequestAsync();
                if (request == null) {
                    MyConsole.WriteLine("Access to device not allowed!");
                    return;
                }
                var writer = new DataWriter();
                writer.ByteOrder = ByteOrder.LittleEndian;
                writer.WriteBytes(reports);

                request.RespondWithValue(writer.DetachBuffer());
            }
        }
            


        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        /// <param name="valueLen">用于检验数据长度是否一致，不一致将自动应答错误</param>
        /// <param name="handle"></param>
        private static async void ProcessWriteRequest(GattLocalCharacteristic sender, GattWriteRequestedEventArgs args, int valueLen, HandleWriteRequest handle) {
            using (args.GetDeferral()) {
                // Get the request information.  This requires device access before an app can access the device's request.
                GattWriteRequest request = await args.GetRequestAsync();
                if (request == null) {
                    // No access allowed to the device.  Application should indicate this to the user.
                    MyConsole.WriteLine("Access to device not allowed!");
                    return;
                }
                if (request.Value.Length != valueLen) {
                    if (request.Option == GattWriteOption.WriteWithResponse) {
                        request.RespondWithProtocolError(GattProtocolError.InvalidAttributeValueLength);
                    }
                    MyConsole.WriteLine("Write request with invalid value length!");
                    return;
                }

                var reader = DataReader.FromBuffer(request.Value);
                reader.ByteOrder = ByteOrder.LittleEndian;

                handle(request, reader);

                if (request.Option == GattWriteOption.WriteWithResponse) {
                    request.Respond();
                    MyConsole.WriteLine("Write responded!");
                }
            }
        }

        private static bool HasSameReportValue(byte[] b0, byte[] b1) {
            if (b0[0] != b1[0]) return false;

            HashSet<byte> hb0 = new HashSet<byte>();
            HashSet<byte> hb1 = new HashSet<byte>();
            for (int i = 2; i <8 ; i++) {
                hb0.Add(b0[i]);
                hb1.Add(b1[i]);
            }
            return hb0.SetEquals(hb1);

        }

        public static void SendByUART(byte[] bs, byte command) {
            if (serialSLIP != null) {
                byte[] bs2 = new byte[bs.Length + 1];
                bs2[0] = command;
                Array.Copy(bs, 0, bs2, 1, bs.Length);
                serialSLIP.SendPacket(bs2);
            }
        }

        //bs.Length=8
        public static async void SendKeysIfChange(byte[] bs) {
            if(HasSameReportValue(bs, iKeyboardReports)) {
                return;
            }
            iKeyboardReports = bs;
            var writer = new DataWriter();
            writer.ByteOrder = ByteOrder.LittleEndian;
            writer.WriteBytes(iKeyboardReports);
            IReadOnlyList<GattClientNotificationResult> ret;
            if (protocolMode == 1) {
                ret=await chrKeyboardInputReport.NotifyValueAsync(writer.DetachBuffer());
            } else {
                ret=await chrBootKeyboardInputReport.NotifyValueAsync(writer.DetachBuffer());
            }


            SendByUART(bs, 0x01);
            
            
            MyConsole.WriteLine($"{protocolMode} Send keys: {iKeyboardReports[0]} {iKeyboardReports[1]} {iKeyboardReports[2]} {iKeyboardReports[3]} " +
                $"{iKeyboardReports[4]} {iKeyboardReports[5]} {iKeyboardReports[6]} {iKeyboardReports[7]}");

            foreach (var result in ret) {
                MyConsole.WriteLine($"{result.Status}");
            }
        }
        
        public static async void SendMouse(byte leftBtn, byte rightBtn, byte midBtn, int dx, int dy, int dwheel) {
            

            byte[] bs=new byte[4];
            if (dx > 127) dx = 127;

            else if (dx < -127) dx = -127;

            if (dy > 127) dy = 127;
            else if (dy < -127) dy = -127;

            if (dwheel > 127) dwheel = 127;
            else if (dwheel < -127) dwheel = -127;

            bs[0] = (byte)((midBtn & 0x01) << 2 | (rightBtn & 0x01) << 1 | (leftBtn & 0x01));

            bs[1] = (byte)dx;
            bs[2] = (byte)dy;
            bs[3] = (byte)dwheel;

            if (iMouseReports[0] == bs[0] && bs[1] == 0 && bs[2] == 0 && bs[3] == 0)
                return;

            SendByUART(bs, 0x02);

            if (clients.Count < 1) {
                //MyConsole.WriteLine("no client!");
                return;
            }


            iMouseReports = bs;
            var writer = new DataWriter();
            writer.ByteOrder = ByteOrder.LittleEndian;
            writer.WriteBytes(iMouseReports);
            IReadOnlyList<GattClientNotificationResult> ret;
            if (protocolMode == 1) {
                ret = await chrMouseInputReport.NotifyValueAsync(writer.DetachBuffer());
            } else {
                ret = await chrBootMouseInputReport.NotifyValueAsync(writer.DetachBuffer());
            }

            MyConsole.WriteLine($"{protocolMode} Send Mouse: {iMouseReports[0]} {iMouseReports[1]} {iMouseReports[2]} {iMouseReports[3]} ");

            foreach (var result in ret) {
                MyConsole.WriteLine($"{result.Status}");
            }
        }

        public static void Stop() {
            if (serviceProvider != null) {
                serviceProvider.StopAdvertising();
            }
        }
    }
}

