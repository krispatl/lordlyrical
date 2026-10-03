import json
import sys
from pathlib import Path
import pytest
sys.path.insert(0,str(Path(__file__).parent))
from show_authority import Registry,ShowAuthority,KEYS

class FakeNet:
    client_no=99
    def __init__(self): self.variables={};self.rpcs=[]
    def set_global_variable(self,n,v): self.variables[n]=v
    def rpc(self,*a):self.rpcs.append(a)

def test_ten_unique_slots_and_keys():
    r=Registry();assert len(set(KEYS))==10
    assert [r.join('device'+str(i),i,100) for i in range(10)]==list(range(10))
    assert r.join('overflow',11,100)==-1
    assert r.join('device3',20,105)==3

def test_reconnect_reserved_slot_and_adaptive_presence():
    r=Registry();r.join('a',1,100)
    assert r.records(107)[0]['connected'] is False
    assert r.join('a',19,108)==0
    assert r.records(109)[0]['clientNo']==19
    assert sum(x['connected'] for x in r.records(109))==1

def test_full_cast_cannot_force_start_missing_clients():
    a=ShowAuthority(FakeNet(),mode='FullCast')
    with pytest.raises(ValueError):a.command('force_start')

def test_reset_increments_run_and_clears_epoch():
    a=ShowAuthority(FakeNet());a.command('seek',85);assert a.snapshot()['phase']=='Running'
    old=a.run;a.command('reset');assert a.run>old and a.epoch==0 and a.phase=='Ready'

def test_serialization_is_atomic():
    a=ShowAuthority(FakeNet());a.command('seek',85)
    s=json.loads(a.net.variables['ll.show']);assert s['epoch']>0 and s['duration']==121
    assert s['runId']==a.run and s['revision']==a.revision and s['protocol']==8

@pytest.mark.parametrize('value',[-1,122,float('nan'),float('inf')])
def test_invalid_seek_rejected(value):
    with pytest.raises(ValueError):ShowAuthority(FakeNet()).command('seek',value)

def test_active_slots_cannot_be_released():
    a=ShowAuthority(FakeNet());a.command('seek',85)
    with pytest.raises(ValueError):a.command('release_absent')

def test_cue_table_and_vfx_coverage():
    cues=json.loads((Path(__file__).parents[1]/'Assets/LordLyrical/Resources/Cues.json').read_text())['cues']
    assert len({c['id'] for c in cues})==len(cues)
    assert [c['time'] for c in cues]==sorted(c['time'] for c in cues)
    assert cues[-1]['time']==121
    assert set(range(1,31))=={v for c in cues for v in c['vfx']}
    assert {0,21,26,39,59,63,69,81,90,98,111,121}<={c['time'] for c in cues}
