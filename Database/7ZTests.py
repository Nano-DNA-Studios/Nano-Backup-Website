import py7zr
import shutil
from py7zr.callbacks import ExtractCallback
import os
from dotenv import load_dotenv
import psycopg2
from tqdm import tqdm

BASEPATH = r"E:\Backups\Important Backup\School\Class Backup SQL Test"
#BASEPATH = r"D:\Class Backup SQL Test"

QueryFormat = """
        INSERT INTO nanobackupdatabase (name, is_file, is_7z, size_bytes, path, parent_id, parent_7z)
        VALUES (%s, %s, %s, %s, %s, %s, %s)
        RETURNING id;
    """

class SevenZipProgress(ExtractCallback):
    def __init__(self, pbar):
        self.pbar = pbar

    def report_start_preparation(self) -> None:
        """Called when starting to scan files."""
        pass

    def report_start(self, processing_file_path: str, processing_bytes: str) -> None:
        """Called when a specific archive starts processing."""
        pass

    def report_update(self, decompressed_bytes: int) -> None:
        """Updates the progress bar with new bytes."""
        # Note: Even if the hint says 'str', it passes numbers for math
        self.pbar.update(int(decompressed_bytes))

    def report_end(self, processing_file_path: str, wrote_bytes: str) -> None:
        """Called when an archive finishes."""
        pass

    def report_warning(self, message: str) -> None:
        """Called for warnings."""
        print(f"\nWarning: {message}")

    def report_postprocess(self) -> None:
        """Called for setting permissions/symlinks."""
        pass

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


def ExtractData(connect, path:str, parentID, ID7z):
    
    if (not os.path.exists(path)):
        return
    
    virtualPath = GetVirtualPath(path)
    name = os.path.basename(virtualPath)
    isFile = os.path.isfile(path)
    
    if (isFile):
        fileSizeBytes = os.path.getsize(path)
    else:
        fileSizeBytes = GetDirectorySize(path)
    
    if (os.path.isfile(path) and path.endswith(".7z")):
        
        writePath = os.path.dirname(path)
        
        print(f"Extracting Folder : {writePath}")
        
        with py7zr.SevenZipFile(path, mode="r") as archive:
            total_size = archive.archiveinfo().uncompressed
        
            with tqdm(total=total_size, unit='B', unit_scale=True, desc="Extracting") as pbar:
                archive.extractall(path=writePath, callback=SevenZipProgress(pbar))
                #archive.close()
        
        newBasePath = path.removesuffix(".7z")
        
        virtualPath = GetVirtualPath(newBasePath)
        name = os.path.basename(virtualPath)
        isFile = os.path.isfile(newBasePath)
        fileSizeBytes = GetDirectorySize(newBasePath)
        
        with connect.cursor() as cur:
            cur.execute(QueryFormat, (name.removesuffix(".7z"), isFile, True, fileSizeBytes, virtualPath, parentID, ID7z))
            newID7z = cur.fetchone()[0]
            connect.commit()
            
            print(f"Wrote 7Z ({newID7z}) {virtualPath}")
        
        for child in os.listdir(newBasePath):
            childPath = os.path.join(newBasePath, child)
            ExtractData(conn, childPath, newID7z, newID7z)
        
        shutil.rmtree(newBasePath)
        
        print(f"Removed Extracted Folder : {newBasePath}")
        
        return
    
    # Write the Data to the SQL Database
    if (os.path.isfile(path)):
        with connect.cursor() as cur:
            cur.execute(QueryFormat, (name, isFile, False, fileSizeBytes, virtualPath, parentID, ID7z))
            fileID = cur.fetchone()[0]
            connect.commit()
            
            print(f"Wrote File ({fileID}) {virtualPath}")
        
        return
    
    # Write the Directory Infos
    with connect.cursor() as cur:
        cur.execute(QueryFormat, (name, isFile, False, fileSizeBytes, virtualPath, parentID, ID7z))
        directoryID = cur.fetchone()[0]
        connect.commit()
        
        print(f"Wrote Folder ({directoryID}) {virtualPath}")
    
    for child in os.listdir(path):
        childPath = os.path.join(path, child)
        ExtractData(conn, childPath, directoryID, ID7z)
    
load_dotenv()

conn = psycopg2.connect(
    dbname="nanobackupwebsite",
    user="postgres",
    password=os.getenv("POSTGRESPASSWORD"),
    host="localhost",
    port="5433"
)

CleanDatabase(conn)

ExtractData(conn, BASEPATH, None, None)
