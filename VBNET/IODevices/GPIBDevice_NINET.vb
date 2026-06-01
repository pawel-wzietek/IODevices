
Imports System

Imports System.Diagnostics
Imports System.Collections
Imports System.Collections.Generic
Imports System.Threading

Imports NationalInstruments.NI4882


Namespace IODevices

    Public Class GPIBDevice_NINET
        Inherits IODevice


        Public NIDevice As NationalInstruments.NI4882.Device
        Public ReadOnly Property NIBoard() As NationalInstruments.NI4882.Board
            Get
                Return boards(gpibboard)
            End Get
        End Property

        Protected gpibaddress As Byte
        'as in NIDevice
        Protected gpibboard As Integer


        'default
        Private Const defaulttimeoutcode As TimeoutValue = TimeoutValue.T1s
        Private CrLf As String = System.Text.Encoding.UTF8.GetString(New Byte(1) {13, 10})
        'CrLf;
        Private Shared boards As Board()


        'variables and code used by notify

        Private Shared notifylist As List(Of IODevice)
        Private Shared locklist As New Object()
        'list of devices to notify
        Private Shared notifymask As GpibStatusFlags = GpibStatusFlags.ServiceRequest
        'srq line

        Private _enableNotify As Boolean = False
        Public Overrides Property EnableNotify() As Boolean

            Get
                Return _enableNotify
            End Get
            Set(ByVal value As Boolean)
                If Not _enableNotify AndAlso value Then
                    SyncLock locklist
                        If notifycount(gpibboard) = 0 Then
                            boards(gpibboard).Notify(notifymask, AddressOf cbnotify, Nothing)
                        End If
                        notifylist.Add(Me)
                    End SyncLock
                    _enableNotify = True
                End If
                If _enableNotify AndAlso Not value Then
                    SyncLock locklist
                        notifylist.Remove(Me)
                        If notifycount(gpibboard) = 0 Then
                            boards(gpibboard).Notify(0, AddressOf cbnotify, Nothing)
                        End If
                    End SyncLock
                    _enableNotify = False
                End If
            End Set
        End Property


        Private Shared Function notifycount(ByVal boardnum As Integer) As Integer
            Dim count As Integer = 0
            For Each device As GPIBDevice_NINET In notifylist
                If boardnum = device.gpibboard Then
                    count += 1
                End If
            Next

            Return count
        End Function

        'notify callback: public delegate void NotifyCallback(Object sender,NotifyData notifyData)
        Public Shared Sub cbnotify(ByVal sender As [Object], ByVal notifyData As NotifyData)

            SyncLock locklist
                For Each device As GPIBDevice_NINET In notifylist
                    If sender.Equals(device.NIBoard) Then
                        device.WakeUp() 'interrupt waiting for next read/poll trial 
                    End If
                Next
            End SyncLock
            Thread.Sleep(1)

            notifyData.SetReenableMask(notifymask)  'rearming to allow next notify 
        End Sub



        Shared Sub New()
            'static constructor: define empty board list and notify list
            boards = New Board(9) {}
            'max 10 boards?
            For i As Integer = 0 To boards.Length - 1
                boards(i) = Nothing
            Next


            notifylist = New List(Of IODevice)()
        End Sub



        '******************
        Public Property BufferSize() As Integer
            Get
                Return NIDevice.DefaultBufferSize
            End Get
            Set(ByVal value As Integer)
                NIDevice.DefaultBufferSize = value
            End Set
        End Property
        Public Sub New(ByVal name As String, ByVal addr As String)
            MyBase.New(name, addr)


            create(name, addr, 32 * 1024)
        End Sub

        Public Sub New(ByVal name As String, ByVal addr As String, ByVal defaultbuffersize As Integer)
            MyBase.New(name, addr)


            create(name, addr, defaultbuffersize)
        End Sub
        'common part of constructor

        Private Sub create(ByVal name As String, ByVal addr As String, ByVal defaultbuffersize As Integer)

            'try to create device (but don't catch catch exceptions in constructor to avoid creating ill-defined objects)


            IODevice.ParseGpibAddr(addr, gpibboard, gpibaddress)

            'added in version of Apr 2017:
            If boards(gpibboard) Is Nothing Then
                statusmsg = "trying to initialize NI board n°" & gpibboard
                boards(gpibboard) = New Board(gpibboard)
                ' can add here other board initialization (IFC etc.)   
                boards(gpibboard).UseAutomaticSerialPolling = False ''apparently necessary for Notify to be reliable
                boards(gpibboard).SynchronizeCallbacks = False  'don't need since the callback is thread-safe
            End If

            statusmsg = "trying to create device '" & name & "' at address " & gpibaddress.ToString()
            NIDevice = New Device(gpibboard, gpibaddress)
            NIDevice.IOTimeout = defaulttimeoutcode

            NIDevice.DefaultBufferSize = defaultbuffersize

            'EOI configuration
            NIDevice.SetEndOnWrite = True
            NIDevice.TerminateReadOnEndOfString = False


            statusmsg = "sending clear to device " & name
            NIDevice.Clear()

            interfacename = "NINET"
            'modified in 2018:
            interfacelockid = gpibboard  'each NI board has its own bus lock (driver is thread-safe)
            AddToList()
            'register in device list displayed by DevicesForm (is not called by base class constructor to avoid registering ill-defined devices in case an exception occurs here)
            statusmsg = ""

        End Sub


        Protected Overrides Sub DisposeDevice()

            If NIDevice IsNot Nothing Then
                EnableNotify = False
                NIDevice.Dispose()
            End If

        End Sub


        Protected Overrides Function Send(ByVal cmd As String, ByRef errcode As Integer, ByRef errmsg As String) As Integer
            'send cmd, return 0 if ok, 1 if timeout,  other if other error


            Dim retval As Integer = 0

            Try


                'so status=1 tmo on send;
                ' =3 tmo on rcv, =4 other err on send, =6 other err on rcv
                NIDevice.Write(cmd)
            Catch ex As GpibException
                errcode = Convert.ToInt32(ex.ErrorCode)
                errmsg = ex.Message
                'timeout 
                If errcode = 6 Then
                    'send tmo

                    retval = 1
                Else
                    'other error

                    retval = 2


                End If
            Catch ex As Exception
                'curiously some gpib exceptions fall here
                retval = 2
                errmsg = ex.Message
                errcode = Convert.ToInt32((New GpibStatus()).ThreadError)
            End Try

            Return retval
        End Function


        '--------------------------
        Protected Overrides Function PollMAV(ByRef mav As Boolean, ByRef statusbyte As Byte, ByRef errcode As Integer, ByRef errmsg As String) As Integer
            'poll for status, return MAV bit 
            'spoll,  return 0 if ok, 1 if timeout,  other if other error

            Dim retval = 0

            Try

                statusbyte = Convert.ToByte(NIDevice.SerialPoll())


                'status=1 tmo on send,  =3 tmo on rcv, =4 other err on send, =6 other err on rcv
                mav = (statusbyte And MAVmask) <> 0
            Catch ex As GpibException
                errcode = Convert.ToInt32(ex.ErrorCode)
                'error 6 "oper aborted" means timeout
                If ex.ErrorCode = GpibError.IOOperationAborted Then

                    retval = 1
                    'ex.Message not always clear ("aborted" etc.)



                    errmsg = "serial poll timeout"
                Else
                    'other error

                    errmsg = ex.Message
                    retval = 2
                End If
            Catch ex As Exception
                retval = 2
                errmsg = ex.Message

                errcode = Convert.ToInt32((New GpibStatus()).ThreadError)
            End Try

            Return retval

        End Function



        ''--------------------
        Protected Overrides Function ReceiveByteArray(ByRef arr As Byte(), ByRef EOI As Boolean, ByRef errcode As Integer, ByRef errmsg As String) As Integer


            Dim retval = 0

            Try

                arr = NIDevice.ReadByteArray()
                Dim st As New GpibStatus()

                'so status=1 tmo on send;
                ' =3 tmo on rcv, =4 other err on send, =6 other err on rcv
                EOI = (Convert.ToInt32(st.ThreadStatus) And Convert.ToInt32(GpibStatusFlags.[End])) <> 0
            Catch ex As GpibException
                errcode = Convert.ToInt32(ex.ErrorCode)
                errmsg = ex.Message
                'error 6 "oper aborted" means timeout
                If ex.ErrorCode = GpibError.IOOperationAborted Then
                    'other error
                    retval = 1
                Else
                    retval = 2
                End If
            Catch ex As Exception
                retval = 2
                errmsg = ex.Message

                errcode = Convert.ToInt32((New GpibStatus()).ThreadError)
            End Try

            Return retval

        End Function


        Protected Overrides Function ClearDevice(ByRef errcode As Integer, ByRef errmsg As String) As Integer

            Dim retval As Integer = 0



            Try
                NIDevice.Clear()
                retval = 0
            Catch ex As Exception
                retval = 1
                errmsg = ex.Message & CrLf & "cannot clear device"
            End Try

            Return retval
        End Function




    End Class
End Namespace
