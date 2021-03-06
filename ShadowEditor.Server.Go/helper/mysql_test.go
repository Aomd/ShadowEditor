// +build ignore

package helper

import (
	"testing"
)

func TestMySQL(t *testing.T) {
	client, err := NewMySQL("localhost", 3306, "root", "root", "test")
	if err != nil {
		t.Error(err)
		return
	}
	defer client.Close()

	_, err = client.Exec("create table test (name text, age int)")
	if err != nil {
		t.Error(err)
		return
	}

	_, err = client.Exec("insert into test (name, age) values ('xiaoming', 12)")
	if err != nil {
		t.Error(err)
		return
	}

	rows, err := client.Query("select * from test where name='xiaoming'")
	if err != nil {
		t.Error(err)
		return
	}

	for rows.Next() {
		var name string
		var age int
		err := rows.Scan(&name, &age)
		if err != nil {
			t.Error(err)
			return
		}
		t.Logf("name: %v, age: %v", name, age)
	}

	_, err = client.Exec("drop table test")
	if err != nil {
		t.Error(err)
		return
	}
}
