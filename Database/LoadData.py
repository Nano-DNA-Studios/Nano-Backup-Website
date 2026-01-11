import os
import dotenv
from dotenv import load_dotenv
import psycopg2

#BASEPATH = "E:\\Backups\\Important Backup\\School\\University Class Backups"
BASEPATH = "E:\Backups\Important Backup\School\Class Backup SQL Test"

QueryFormat = """
        INSERT INTO nanobackupdatabase (name, is_file, size_bytes, path, parent_id, file_data)
        VALUES (%s, %s, %s, %s, %s, %s)
        RETURNING id;
    """


def GetDirectorySize(directory):
    total_size = 0
    # os.walk goes through every sub-folder
    for root, dirs, files in os.walk(directory):
        for f in files:
            fp = os.path.join(root, f)
            # Skip if it's a symbolic link (to avoid double counting or errors)
            if not os.path.islink(fp):
                total_size += os.path.getsize(fp)
    return total_size

def CleanDatabase(connect:"connection"):
    with connect.cursor() as cur:
        cur.execute("TRUNCATE TABLE nanobackupdatabase RESTART IDENTITY CASCADE;")

def LoadRootFolder(connect) -> int:
    
    with connect.cursor() as cur:
        cur.execute(QueryFormat, ("Class Backups", False, GetDirectorySize(BASEPATH), "./", None, None))
        root_id = cur.fetchone()[0]
        print(root_id)

def GetVirtualPath(path: str):
    return path.replace(BASEPATH, "./Class Backups")
    
def WriteData(connect, path, parentID):
    
    if (not os.path.exists(path)):
        return
    
    virtualPath = GetVirtualPath(path)
    name = os.path.basename(virtualPath)
    isFile = os.path.isfile(path)
    
    if (isFile):
        fileSizeBytes = os.path.getsize(path)
    else:
        fileSizeBytes = GetDirectorySize(path)
    
    # Check if Folder, Add it to the database
    if (os.path.isfile(path)):
        
        with connect.cursor() as cur:
            oid = None
            lobj = connect.lobject(0, 'wb', 0) # WB = Write Bytes
            with open(path, 'rb') as f:
                lobj.write(f.read())
                
                #fileData = f.read()
            oid = lobj.oid
            lobj.close()
            cur.execute(QueryFormat, (name, isFile, fileSizeBytes, virtualPath, parentID, oid))
            
            fileID = cur.fetchone()[0]
            connect.commit()
            
            print(f"Wrote File ({fileID}) {virtualPath}")
            
        return
    
    # Write the Directory Infos
    with connect.cursor() as cur:
        cur.execute(QueryFormat, (name, isFile, fileSizeBytes, virtualPath, parentID, None))
        directoryID = cur.fetchone()[0]
        connect.commit()
        
        print(f"Wrote Folder ({directoryID}) {virtualPath}")
    
    for child in os.listdir(path):
        childPath = os.path.join(path, child)
        WriteData(connect, childPath, directoryID)

load_dotenv()

conn = psycopg2.connect(
    dbname="nanobackupwebsite",
    user="postgres",
    password=os.getenv("POSTGRESPASSWORD"),
    host="localhost",
    port="5433"
)

CleanDatabase(conn)
WriteData(conn, BASEPATH, None)
