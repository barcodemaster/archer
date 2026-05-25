' =============================================================
' ExportToCSV VBA Macro for UpgradeData.xlsx
' UTF-8 (BOM 없음) CSV 내보내기
' =============================================================
' 사용법:
' 1. Assets/Data/ 폴더에 UpgradeData.xlsx 생성 (매크로 사용 시 .xlsm으로 저장)
' 2. Sheet1에 아래 헤더와 데이터 입력:
'    A1: type | B1: name | C1: description | D1: icon
' 3. Alt+F11 -> VBAProject -> 모듈 삽입 -> 이 코드 붙여넣기
' 4. 시트에 버튼 추가: 개발 도구 > 삽입 > 버튼 > ExportToCSV 매크로 연결
' =============================================================

Sub ExportToCSV()
    Dim ws As Worksheet
    Dim lastRow As Long
    Dim lastCol As Long
    Dim csvPath As String
    Dim i As Long, j As Long
    Dim lineStr As String
    Dim cellValue As String

    Set ws = ThisWorkbook.Sheets(1)

    ' 데이터 범위 확인
    lastRow = ws.Cells(ws.Rows.Count, 1).End(xlUp).Row
    lastCol = ws.Cells(1, ws.Columns.Count).End(xlToLeft).Column

    ' 출력 경로: ThisWorkbook 기준 상대 경로
    csvPath = ThisWorkbook.Path & "\..\Resources\Data\UpgradeData.csv"

    ' ADODB.Stream으로 UTF-8 (BOM 없이) 저장
    Dim stream As Object
    Set stream = CreateObject("ADODB.Stream")
    stream.Type = 2 ' adTypeText
    stream.Charset = "UTF-8"
    stream.Open

    For i = 1 To lastRow
        lineStr = ""
        For j = 1 To lastCol
            cellValue = CStr(ws.Cells(i, j).Value)

            ' 쉼표나 줄바꿈이 포함된 경우 따옴표로 감싸기
            If InStr(cellValue, ",") > 0 Or InStr(cellValue, vbLf) > 0 Or InStr(cellValue, """") > 0 Then
                cellValue = """" & Replace(cellValue, """", """""") & """"
            End If

            If j = 1 Then
                lineStr = cellValue
            Else
                lineStr = lineStr & "," & cellValue
            End If
        Next j

        ' 마지막 행 뒤에도 줄바꿈 추가 (기존 CSV 형식 유지)
        stream.WriteText lineStr & vbCrLf
    Next i

    ' BOM 제거를 위해 바이너리로 변환 후 저장
    stream.Position = 0

    Dim binaryStream As Object
    Set binaryStream = CreateObject("ADODB.Stream")
    binaryStream.Type = 1 ' adTypeBinary
    binaryStream.Open

    ' UTF-8 BOM (3바이트) 건너뛰기
    stream.Position = 0
    stream.Type = 1 ' Switch to binary
    stream.Position = 3 ' Skip BOM (EF BB BF)

    Dim byteData() As Byte
    byteData = stream.Read

    binaryStream.Write byteData
    binaryStream.SaveToFile csvPath, 2 ' adSaveCreateOverWrite

    binaryStream.Close
    stream.Close

    Set binaryStream = Nothing
    Set stream = Nothing

    MsgBox "CSV 내보내기 완료!" & vbCrLf & csvPath, vbInformation, "ExportToCSV"
End Sub
