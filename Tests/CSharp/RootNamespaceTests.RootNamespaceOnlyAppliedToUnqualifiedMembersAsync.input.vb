 'Comment from start of file moves within the namespace
Class AClassInRootNamespace ' Becomes nested - 1
End Class ' Becomes nested - 2

Namespace Global.NotNestedWithinRoot
    Class AClassInANamespace
    End Class
End Namespace

Namespace NestedWithinRoot
    Class AClassInANamespace
    End Class
End Namespace