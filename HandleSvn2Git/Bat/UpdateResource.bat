@rem @set SourcePath=F:\SyncProjects\ShuiHu\hqsh180_20170228

@rem @set SourcePath=F:\SyncProjects\ShuiHu\tanyu180_20170224
@rem @set DestPath=F:\SyncProjects\ShuiHu\gitWorks\ShuiHu

@rem rmdir /s/q %DestArtPath% 

@rem @echo Í¬²½Ä¿Â¼

@rem @python ./PythonScripts/SyncFolder.py %SourcePath% %DestPath%

@rem HandleTrunk
@set svnShuiHu=F:\SyncProjects\ShuiHu\trunk
@set gitShuiHu=F:\SyncProjects\ShuiHu\gitWorks\ShuiHu\ShuiHu_trunk
@set branchName=trunk
@set compName=CheckSVN2Git_ShuiHu_trunk
@set threadNum=100

@HandleSvn2Git.exe %svnShuiHu% %gitShuiHu% %branchName% %compName% %threadNum%




Pause