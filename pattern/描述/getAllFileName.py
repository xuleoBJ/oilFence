# -*- coding: cp936 -*-
import os
import shutil


if __name__=="__main__":
    
    sourceDirPath="����Ԫ��Miall1985"

    fileWrited_txtLog=open("outPut.txt",'w')
    fileNames=os.listdir(sourceDirPath)
    for wellItem in fileNames:
        fileWrited_txtLog.write(sourceDirPath+"\t"+wellItem[:-4]+"\n")

    fileWrited_txtLog.close()    

    print("�������")
