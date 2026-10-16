# Request

Make NPCs for more interaction. 
For not just make them in cilindar shapes.

## NPC 1: checkout counter
이사람은 계산대에 서있는 사람이야. 일정 거리 가까이에 가면 'Check out?' 팝업이 뜨고, 그러면 현재 장바구니 아이템 리스트를 다시 UI에 보여주고, UI 아래에는 왼쪽에 Shop more / 오른쪽에 Check out 버튼이 있도록 해줘. 그리고 Check out을 누르면 Thanks for shopping! 문구가 뜨고 장바구니가 초기화되는거야.

## NPC 2: personal shopper
이사람은 가게 코너에 서있는 사람이야. occationally 도움이 필요하면 말씀해주세요~ 아니면 Welcome to the shop! 이런 문구가 팝업으로 떴다가 사라져(한 30초에 한번씩 뜬다고 하자)

그리고 가까이에 가면은 말을 걸 수 있는데, 말을 걸 수 있다는 것도 커서로 indicate해줘. 말을 걸면 'How can I help?' 하고 answer로 'Help me choose outfit!' or 'never mind' 두개가 떠. 그리고 첫번째 'help me choose outfit!' 하면은 이제 이사람이 따라다니면서 내가 옷을 선택하면 약간 옷에 대한 코멘트를 해주는거야. 코멘트는 옷 상세보기를 했을때 왼쪽 영역에 말풍선처럼 뜨게되어있어. 

그리고 계속 졸졸 따라다니다가 다시 말을 걸면은 'keep help me' 'stop following me' 이렇게 옵션인고 stop following me 하면은 그사람은 원래 있던 자리로 돌아가.


위에 말한거로 NPC오브젝트와 인터랙션 스크립트를 만들어줘.