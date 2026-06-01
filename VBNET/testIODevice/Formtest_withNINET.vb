Imports IODevices


Public Class Formtest_withNINET



    Public dev1 As IODevice
    Public dev2 As IODevice

    Sub formtest_close()
        IODevice.DisposeAll()
    End Sub

    Private Sub Formtest_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load

        lstIntf1.Items.Add("GPIB:NI-NET")
        lstIntf1.Items.Add("Visa")
        lstIntf1.Items.Add("GPIB:ADLink")
        lstIntf1.Items.Add("gpib488.dll")
        lstIntf1.Items.Add("Com port")
        lstIntf1.SelectedIndex = 0


        lstIntf2.Items.Add("GPIB:NI-NET")
        lstIntf2.Items.Add("Visa")
        lstIntf2.Items.Add("GPIB:ADLink")
        lstIntf2.Items.Add("gpib488.dll")
        lstIntf2.Items.Add("Com port")
        lstIntf2.SelectedIndex = 0
    End Sub
    Function CreateDevice(ByVal name As String, ByVal address As String, ByVal interfacetype As Integer) As IODevice

        'this function returns a generic IODevice object: here we can define devices polymorphically 
        ' because in this test code we don't use many interface-specific methods

        'this is convenient if there are not too many interface-dependent options  (eg visa attributes, see below)
        '(but otherwise it is still possible to bind statically without any change in the program other than initialization)


        Dim dev As IODevice

        Try

            Select Case interfacetype
                Case 0 : dev = New GPIBDevice_NINET(name, address)
                Case 1 : dev = New VisaDevice(name, address)
                Case 2 : dev = New GPIBDevice_ADLink(name, address)
                Case 3 : dev = New GPIBDevice_gpib488(name, address)
                Case 4 : dev = New SerialDevice(name, address)
                Case Else : Return Nothing
            End Select



        Catch ex As Exception  '(constructor exception: e.g. "dll not found" or error in board initialization)
            Dim msg As String = " cannot create device " & name & vbCrLf & ex.Message
            If ex.InnerException IsNot Nothing AndAlso ex.Message <> ex.InnerException.Message Then msg = msg & vbCrLf & ex.InnerException.Message
            MessageBox.Show(msg)
            dev = Nothing
            IODevice.statusmsg = ""
        End Try

        'option to debug interface routines, may remove this line in final version:
        'If dev IsNot Nothing Then dev.catchinterfaceexceptions = False 

        Return dev


    End Function


    Private Sub setnotify(ByVal dev As IODevice)

        Try
            dev.EnableNotify = True 'default implementation will throw an exception if not available for the selected interface
            Dim result As Integer = dev.SendBlocking("*SRE 16", False) ' set bit 4 in Service Request Enable Register, so that the MAV status will set SRQ
            If result = 0 Then
                dev.delayread = 1000 'set long wait delays (will be interrupted anyway)
                dev.delayrereadontimeout = 1000
            End If
        Catch ex As Exception
            Dim msg As String = " cannot set EnableNotify for device " & dev.devname & vbCrLf & ex.Message
            If ex.InnerException IsNot Nothing Then
                msg = msg & vbCrLf & ex.InnerException.Message
            End If
            MessageBox.Show(msg)
        End Try


    End Sub



    Private Sub btncreate_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btncreate.Click

        dev1 = CreateDevice(txtname1.Text, txtaddr1.Text, lstIntf1.SelectedIndex)
        dev2 = CreateDevice(txtname2.Text, txtaddr2.Text, lstIntf2.SelectedIndex)



        'to simulate slow response from device 2 we can set for example:
        'dev2.delayread = 2000;

        'example of interface-specific action: setting visa options in case visa is selected for dev1:
        Dim d As VisaDevice = TryCast(dev1, VisaDevice)
        'for example set some attributes:
        ' d.SetAttribute(1, 0);
        If d IsNot Nothing Then
        End If



        If dev1 IsNot Nothing Then
            gbox1.Enabled = True

            'examples of some settings
            dev1.maxtasks = 10
            dev1.readtimeout = 5000

            dev1.showmessages = True

            'dev1.enablepoll = False  'uncomment this if a device does not support polling ("poll timeout" is signalled)

            'dev1.MAVmask = ...  'set a different mask if your device supports polling but its status flags do not comply with 488.2


            'uncomment this to use SRQ notifying on device1 :
            'setnotify(dev1)

            dev1.catchcallbackexceptions = True
        End If

        If dev2 IsNot Nothing Then

            gbox2.Enabled = True

            'uncomment this to use SRQ notifying on device2 :
            'setnotify(dev2)

        End If



        btncreate.Enabled = False
        gboxdev.Enabled = False
        btndevlist.Enabled = True

    End Sub

    Private Sub btndevlist_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btndevlist.Click
        IODevice.ShowDevices()
    End Sub

    Private Sub btnq1b_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnq1b.Click

        Dim q As IOQuery = Nothing
        Dim result As Integer

        Dim s As String

        btnq1b.Enabled = False


        result = dev1.QueryBlocking(txtq1b.Text, q, True) 'standard version with IOQuery parameter
        btnq1b.Enabled = True

        s = "blocking command:'" & q.cmd & "'" & vbCrLf

        If result = 0 Then

            txtr1b.Text = q.ResponseAsString
            s &= "device response time:" & Str(q.timeend.Subtract(q.timestart).TotalSeconds) & " s" & vbCrLf
            s &= "thread wait time:" & Str(q.timestart.Subtract(q.timecall).TotalSeconds) & " s" & vbCrLf


        Else
            s &= "status: " & result & vbCrLf
            s &= q.errmsg
        End If

        txtr1astat.Text = s
      

    End Sub


    Private Sub btnq2b_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnq2b.Click

        Dim resp As String = Nothing
        Dim result As Integer

        btnq2b.Enabled = False
        result = dev2.QueryBlocking(txtq2b.Text, resp, True) 'simpler version with string parameter
        btnq2b.Enabled = True

        If result = 0 Then
            txtr2b.Text = resp
        End If
    End Sub

    Private Sub btnq1a_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnq1a.Click


        If dev1.PendingTasks(txtq1a.Text) <= 3 Then
            'example of using PendingTasks() method
            dev1.QueryAsync(txtq1a.Text, AddressOf cbdev1, True)
        Else
            txtr1astat.Text = "already 3 '" & txtq1a.Text & "' commands pending"
        End If

    End Sub

    Private Sub btnq2a_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnq2a.Click
        dev2.QueryAsync(txtq2a.Text, AddressOf cbdev2, True)
    End Sub

    ''signature of callback functions:   Public Delegate Sub IOCallback(ByVal q As IOQuery) 

    Sub cbdev1(ByVal q As IOQuery)



        Try


            Dim s As String = "async command:'" & q.cmd & "'" & vbCrLf
            If q.status = 0 Then
                txtr1a.Text = q.ResponseAsString

            Else
                s &= "error " & q.errcode & vbCrLf
            End If

            s &= "device response time:" & Str(q.timeend.Subtract(q.timestart).TotalSeconds) & " s" & vbCrLf
            s &= "thread wait time:" & Str(q.timestart.Subtract(q.timecall).TotalSeconds) & " s" & vbCrLf

            txtr1astat.Text = s

            'uncomment this to chain on dev2:
            'dev2.QueryAsync(txtq2a.Text, AddressOf cbdev2, True)

        Catch ex As Exception
            txtr1astat.Text = "error in callback function:" & vbCrLf
            txtr1astat.Text &= ex.Message & vbCrLf
            If ex.InnerException IsNot Nothing Then txtr1astat.Text &= ex.InnerException.Message
        End Try
    End Sub


    Sub cbdev2(ByVal q As IOQuery)

        Try
            Dim s As String = "async command:'" & q.cmd & "'" & vbCrLf
            If q.status = 0 Then
                txtr2a.Text = q.ResponseAsString

              
            End If


            s &= "device response time:" & Str(q.timeend.Subtract(q.timestart).TotalSeconds) & " s" & vbCrLf
            s &= "thread wait time:" & Str(q.timestart.Subtract(q.timecall).TotalSeconds) & " s" & vbCrLf

            txtr2astat.Text = s

            'uncomment this to chain on dev1:
            'dev1.QueryAsync(txtq1a.Text, AddressOf cbdev1, True)

        Catch ex As Exception
            txtr2astat.Text = "error in callback function:" & vbCrLf
            txtr2astat.Text &= ex.Message & vbCrLf
            If ex.InnerException IsNot Nothing Then txtr2astat.Text &= ex.InnerException.Message
        End Try


    End Sub

    Private Sub Formtest_FormClosing(ByVal sender As System.Object, ByVal e As System.Windows.Forms.FormClosingEventArgs) Handles MyBase.FormClosing

        'shut down gracefully: (may take time)
        IODevice.DisposeAll()

    End Sub

    'suggest correct address format:
    Sub formataddr(ByVal interfacenum As Integer, ByVal txtaddr As TextBox)

        Dim sa As String = Trim(txtaddr.Text)
        Select Case interfacenum
            Case 0, 2, 3
                If Not IsNumeric(sa) Then
                    txtaddr.Text = "1"

                End If
            Case 1
                If IsNumeric(sa) Then
                    txtaddr.Text = "GPIB0::" & sa & "::INSTR"
                Else
                    txtaddr.Text = "GPIB0::1::INSTR"
                    '   or USB format like:  "USB0::xxxx::xxxx::xxxx::INSTR"

                End If
            Case 4
                If IsNumeric(sa) Then
                    txtaddr.Text = "COM" & sa & ":9600,N,8,1,CRLF"
                Else
                    txtaddr.Text = "COM1:9600,N,8,1,CRLF"

                End If
                'Case Else
        End Select


    End Sub


    Private Sub lstIntf1_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles lstIntf1.SelectedIndexChanged

        formataddr(lstIntf1.SelectedIndex, txtaddr1)

    End Sub

    Private Sub lstIntf2_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles lstIntf2.SelectedIndexChanged

        formataddr(lstIntf2.SelectedIndex, txtaddr2)



    End Sub

End Class


