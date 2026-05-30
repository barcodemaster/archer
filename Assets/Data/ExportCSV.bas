Attribute VB_Name = "ExportCSV"
Option Explicit

'/**
' * ExportAllCSV
' * 워크북 내 화이트리스트에 등록된 시트를 순회하여
' * Assets/Resources/Data/{시트이름}.csv 로 UTF-8(BOM 없음) CSV를 내보냅니다.
' * 새 시트를 추가하려면 sheetNames 배열에 이름만 추가하세요.
' */
Public Sub ExportAllCSV()
    Dim sheetNames As Variant
    sheetNames = Array("UpgradeData", "EquipmentData")

    Dim basePath As String
    ' xlsm 파일 위치(Assets/Data/) 기준 상대경로 -> Assets/Resources/Data/
    basePath = ThisWorkbook.Path & "\..\Resources\Data\"

    ' 폴더가 없으면 생성
    If Dir(basePath, vbDirectory) = "" Then
        MkDir basePath
    End If

    Dim exported As Long
    exported = 0

    Dim i As Long
    For i = LBound(sheetNames) To UBound(sheetNames)
        Dim sheetName As String
        sheetName = sheetNames(i)

        ' 시트 존재 확인
        Dim ws As Worksheet
        Set ws = Nothing
        On Error Resume Next
        Set ws = ThisWorkbook.Sheets(sheetName)
        On Error GoTo 0

        If ws Is Nothing Then
            ' 시트가 없으면 건너뜀
        Else
            Dim filePath As String
            filePath = basePath & sheetName & ".csv"

            Call ExportSheetToCSV(ws, filePath)
            exported = exported + 1
        End If
    Next i

    MsgBox exported & "개 시트를 CSV로 내보냈습니다." & vbCrLf & _
           "경로: " & basePath, vbInformation, "ExportAllCSV"
End Sub

'/**
' * ExportSheetToCSV
' * 단일 시트를 UTF-8(BOM 없음) CSV 파일로 내보냅니다.
' * 빈 행은 건너뜁니다.
' */
Private Sub ExportSheetToCSV(ws As Worksheet, filePath As String)
    Dim lastRow As Long
    Dim lastCol As Long
    lastRow = ws.Cells(ws.Rows.Count, 1).End(xlUp).Row
    lastCol = ws.Cells(1, ws.Columns.Count).End(xlToLeft).Column

    If lastRow < 1 Or lastCol < 1 Then Exit Sub

    ' ADODB.Stream을 사용하여 UTF-8(BOM 없음) 출력
    Dim stream As Object
    Set stream = CreateObject("ADODB.Stream")
    stream.Type = 2 ' adTypeText
    stream.Charset = "UTF-8"
    stream.Open

    Dim r As Long
    Dim c As Long
    For r = 1 To lastRow
        ' 빈 행 건너뛰기: 첫 번째 셀이 비어있으면 스킵
        If Trim(ws.Cells(r, 1).Value & "") = "" Then GoTo NextRow

        Dim line As String
        line = ""
        For c = 1 To lastCol
            Dim cellVal As String
            cellVal = ws.Cells(r, c).Value & ""

            ' 쉼표나 줄바꿈, 쌍따옴표가 포함된 경우 이스케이프
            If InStr(cellVal, ",") > 0 Or InStr(cellVal, vbLf) > 0 Or _
               InStr(cellVal, vbCr) > 0 Or InStr(cellVal, """") > 0 Then
                cellVal = """" & Replace(cellVal, """", """""") & """"
            End If

            If c = 1 Then
                line = cellVal
            Else
                line = line & "," & cellVal
            End If
        Next c

        stream.WriteText line, 1 ' 1 = adWriteLine
NextRow:
    Next r

    ' BOM 제거: UTF-8 스트림을 바이너리로 변환하며 BOM(3바이트) 스킵
    stream.Position = 0

    Dim binStream As Object
    Set binStream = CreateObject("ADODB.Stream")
    binStream.Type = 1 ' adTypeBinary
    binStream.Open

    ' UTF-8 BOM(3바이트) 건너뛰기
    stream.Type = 1 ' switch to binary
    stream.Position = 3

    Dim bytes() As Byte
    bytes = stream.Read
    binStream.Write bytes

    binStream.SaveToFile filePath, 2 ' 2 = adSaveCreateOverWrite

    binStream.Close
    stream.Close

    Set binStream = Nothing
    Set stream = Nothing
End Sub
