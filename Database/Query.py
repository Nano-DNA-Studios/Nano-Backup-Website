import psycopg2
import psycopg2.extras
import os
from dotenv import load_dotenv

def get_folder_structure(conn):
    cur = conn.cursor(cursor_factory=psycopg2.extras.DictCursor)
    cur.execute("SELECT * FROM nanobackupdatabase")
    rows = cur.fetchall()

    # Create a map for quick lookup
    nodes = {row['id']: dict(row) for row in rows}
    forest = []

    for node_id, node in nodes.items():
        parent_id = node['parent_id']
        print(f"{parent_id} : {node["id"]} : {node["name"]} : {node["path"]}")
        
        if parent_id is None:
            forest.append(node)
        else:
            parent = nodes.get(parent_id)
            if parent:
                if 'children' not in parent:
                    parent['children'] = []
                parent['children'].append(node)
    
    return forest

load_dotenv()

conn = psycopg2.connect(
    dbname="nanobackupwebsite",
    user="postgres",
    password=os.getenv("POSTGRESPASSWORD"),
    host="localhost",
    port="5433"
)

get_folder_structure(conn)