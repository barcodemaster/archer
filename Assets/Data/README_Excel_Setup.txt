=====================================================
 UpgradeData Excel + VBA 매크로 설정 가이드
=====================================================

[1] Excel 파일 생성
-----------------------------------------------------
1. 이 폴더(Assets/Data/)에서 새 Excel 파일 생성
2. 파일명: UpgradeData.xlsm (매크로 사용 Excel 파일)
3. Sheet1에 다음과 같이 데이터 입력:

   A열(type)      B열(name)      C열(description)           D열(icon)          E열(maxLevel)
   ───────────────────────────────────────────────────────────────────────────────────────────
   type           name           description                icon               maxLevel
   AttackSpeed    공격 속도       공격 속도가 15% 증가합니다    icon_attack_speed   5
   FrontArrow     전방 화살       전방에 화살을 추가합니다      icon_front_arrow    3
   MultiShot      멀티샷         연속으로 화살을 발사합니다     icon_multishot      3
   Piercing       관통           화살이 적을 관통합니다        icon_piercing       1
   Headshot       헤드샷         즉사 확률 8% 증가            icon_headshot       5
   DiagonalArrow  대각선 화살     45도 방향으로 화살 추가       icon_diagonal       1
   WallBounce     벽 반사        화살이 벽에 반사됩니다        icon_wallbounce     1
   SideArrow      측면 화살       90도 방향으로 화살 추가       icon_side_arrow     1
   Ricochet       도탄           적 사이를 튕기는 화살         icon_ricochet       3
   BackArrow      후방 화살       뒤로도 화살을 발사합니다      icon_back_arrow     1

   * 1행은 반드시 헤더 (type, name, description, icon, maxLevel)
   * A열 빈 셀이 나오면 해당 행까지만 내보냄
   * E열(maxLevel): 해당 능력의 최대 레벨. 1이면 1회만 획득 가능, 5이면 5회까지 반복 획득 가능

[2] VBA 매크로 추가
-----------------------------------------------------
1. Alt + F11 (VBA 편집기 열기)
2. 왼쪽 프로젝트 탐색기에서 VBAProject(UpgradeData.xlsm) 우클릭
3. 삽입 > 모듈
4. ExportToCSV_VBA.bas 파일 내용을 모듈에 붙여넣기
5. Ctrl + S 저장

[3] 내보내기 버튼 추가 (선택사항)
-----------------------------------------------------
1. 개발 도구 탭 > 삽입 > 양식 컨트롤 > 버튼
2. 시트에 버튼 그리기
3. 매크로 지정 대화상자에서 "ExportToCSV" 선택
4. 버튼 텍스트를 "CSV 내보내기"로 변경

   * 개발 도구 탭이 없으면: 파일 > 옵션 > 리본 사용자 지정 > 개발 도구 체크

[4] 사용법
-----------------------------------------------------
1. UpgradeData.xlsm 열기
2. 데이터 편집 (행 추가/수정/삭제)
3. "CSV 내보내기" 버튼 클릭 (또는 Alt+F8 > ExportToCSV > 실행)
4. Assets/Resources/Data/UpgradeData.csv 자동 갱신
5. Unity 에디터로 돌아가면 자동 임포트됨

[5] 폴더 구조
-----------------------------------------------------
Assets/
├── Data/
│   ├── UpgradeData.xlsm          <- Excel 편집용 (빌드에 미포함)
│   ├── ExportToCSV_VBA.bas       <- VBA 소스 백업
│   └── README_Excel_Setup.txt    <- 이 파일
└── Resources/
    └── Data/
        └── UpgradeData.csv       <- 매크로가 내보내는 CSV (Unity 런타임 사용)

[6] 주의사항
-----------------------------------------------------
- Excel 파일은 반드시 .xlsm (매크로 사용 통합 문서)으로 저장
- .xlsx로 저장하면 매크로가 제거됨
- ADODB.Stream을 사용하므로 Windows 환경 필요
- 매크로 보안: 파일 > 옵션 > 보안 센터 > 매크로 설정 > "모든 매크로 포함" 또는 "알림을 표시"
