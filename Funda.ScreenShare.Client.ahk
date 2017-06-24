SetTitleMatchMode, 2

#IfWinNotActive, Visual Studio
	F1::SnipAndPost()
	^+v::Post()

#IfWinActive, Snipping Tool
	F1::Post()

#IfWinActive
	F1::SnipAndPost()
	CapsLock & 1::CopyAndPost("https://media.giphy.com/media/xUPGcwjbsmlkQFN1cc/giphy.gif?response_id=5921a50f40e827cc7df7d8d411111")

OnClipboardChange:
IfInString, clipboard, giphy
	Post()

Copy(content) {
	clipboard = %content%  
}

Snip() {
    RunWait, snippingtool /clip
}

Post() {
    Run, Funda.ScreenShare.Client.exe
}

SnipAndPost(){
	Snip()
	Post()
}

CopyAndPost(content){
	Copy(content)
	Post()
}

