# -*- coding: cp936 -*-
import os
import shutil
import re

def funReplace(filePathSource,filePathWrited):
        fileOpened_forwardLog=open(filePathSource,'r')
        s=fileOpened_forwardLog.read()
        s=s.replace("width=\"210mm\"","width=\"200\"")
        s=s.replace("height=\"297mm\"","height=\"300\"")
        source=s.replace("0 0 744.09448819 1052.3622047","0 0 200 300")
        #newStr=source.replace('����','������')
        fileWrited_txtLog=open(filePathWrited,'w')
        fileWrited_txtLog.write(source)
        fileOpened_forwardLog.close()
        fileWrited_txtLog.close()
    

if __name__=="__main__":
    
    sourceDirPath="rockSi"
    goalDirPath='New'
    
    print ('prepare: ',goalDirPath)
    if os.path.exists(goalDirPath):
        shutil.rmtree(goalDirPath)
    os.mkdir(goalDirPath)

    ##  �Ѳ���Ŀ¼���ļ�����filenameslist
    fileNames=os.listdir(sourceDirPath)
    for wellItem in fileNames:
        print ('doing'+'-'*10,wellItem)
        fileOpened=sourceDirPath+'\\'+wellItem
        fileNew=wellItem.replace("����","����")
        fileWrited=goalDirPath+'\\'+fileNew
        funReplace(fileOpened,fileWrited)
        

    print("�������")
